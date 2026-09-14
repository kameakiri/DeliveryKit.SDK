using System;
using System.IO;

namespace DeliveryKit.LogArchiver;

class Program
{
    static void Main(string[] args)
    {
        // 配布可能なツールが特定ドライブの絶対パスを前提にするのは環境依存が強すぎるため、
        // 既定値は実行ディレクトリ配下の相対パスにし、環境変数／コマンドライン引数で
        // 上書きできるようにした（Windowsタスクスケジューラでの運用を想定）。
        //   LogArchiver.exe [basePath] [archivePath] [days]
        string basePath = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("DELIVERYKIT_LOG_PATH")
              ?? Path.Combine(AppContext.BaseDirectory, "Logs");

        string archivePath = args.Length > 1
            ? args[1]
            : Environment.GetEnvironmentVariable("DELIVERYKIT_LOG_ARCHIVE_PATH")
              ?? Path.Combine(AppContext.BaseDirectory, "LogsArchive");

        int days = args.Length > 2 && int.TryParse(args[2], out var parsedDays) ? parsedDays : 180;

        // 隔離したログを最終的に削除するまでの日数。
        //
        // 監査で発覚：このツールは「削除ではなく隔離」を売りにしているが、**隔離した先を
        // 片付ける仕組みがどこにも無かった。** 毎日動かすほど LogsArchive だけが増え続け、
        // 次の2つが起きる。
        //  - ディスクが埋まる。App Service のようにローカルディスクが小さい実行環境では、
        //    ログの置き場が満杯になった時点でアプリ自体が書き込みに失敗する。
        //  - **ログに含まれる個人データが、事業者が利用者に約束した保存期間を越えて残る。**
        //    「隔離済み」は「消した」ではない。約束した期間で消していると思い込んだまま
        //    運用できてしまうのが、この構造のいちばん困るところだった。
        //    （本体側の DeliveryKit.Log でも同じ不備が見つかり、隔離先の削除を足している。）
        //
        // 既定は 0（削除しない）で、これまでの動きを変えない。**明示的に指定したときだけ
        // 削除する。** 既に動いている配布先で、更新した途端に黙ってログが消えるのは
        // 「無警告の消失が最も困る」という下の方針に反する。
        int purgeDays = args.Length > 3 && int.TryParse(args[3], out var parsedPurgeDays)
            ? parsedPurgeDays
            : int.TryParse(Environment.GetEnvironmentVariable("DELIVERYKIT_LOG_PURGE_DAYS"), out var envPurgeDays)
              ? envPurgeDays
              : 0;

        // 隔離より早く削除する設定は、隔離という段階そのものを無意味にする（隔離される前に
        // 消える）。設定ミスでログを失うより、削除しない方が安全なので無効として扱う。
        if (purgeDays > 0 && purgeDays < days)
        {
            Console.Error.WriteLine(
                $"[LogArchiver] 削除日数({purgeDays})が隔離日数({days})より短いため、削除は行いません。");
            purgeDays = 0;
        }

        DateTime threshold = DateTime.Now.AddDays(-days);

        // 走査するカテゴリ。
        //
        // 監査で発覚：以前はここに6つの名前を決め打ちしていた。ところが
        // `DeliveryLogger` のカテゴリは**コンストラクタに渡した任意の文字列**で、
        // 決まった一覧があるわけではない（docs/logging.md 参照）。
        // 一覧に無い名前を使うと、**そのログだけ隔離も削除もされないまま
        // 無期限に溜まり続ける。** しかも動かしている側からは、アーカイバが
        // 毎回正常終了して見える。
        //
        // 出力先の直下にあるディレクトリを全て対象にする。中の `*.log` しか
        // 触らないので、ログ以外のものが混ざっていても影響しない。
        string[] categories = Directory.Exists(basePath)
            ? Directory.GetDirectories(basePath).Select(Path.GetFileName).OfType<string>().ToArray()
            : Array.Empty<string>();

        foreach (var category in categories)
        {
            string dir = Path.Combine(basePath, category);

            // ログが一度も書かれていないカテゴリはディレクトリ自体が存在せず、
            // Directory.GetFiles が例外を投げるため、事前にガードする。
            if (!Directory.Exists(dir))
                continue;

            string archiveDir = Path.Combine(archivePath, category);

            foreach (var file in Directory.GetFiles(dir, "*.log"))
            {
                DateTime lastWrite = File.GetLastWriteTime(file);

                if (lastWrite >= threshold)
                    continue;

                try
                {
                    // 移動するファイルがあると分かってから作る。無条件に作ると、
                    // 対象が1件も無いカテゴリにも空のフォルダが残る。
                    Directory.CreateDirectory(archiveDir);

                    string dest = Path.Combine(archiveDir, Path.GetFileName(file));

                    // 同名が既にある場合は移動しない。File.Move(file, dest) は
                    // 上書きせず例外を投げるが、**中身を検証せず上書きするより、
                    // 元ファイルを残して次回に回す方が安全**である。ログは追記専用で、
                    // 無警告の消失が最も困る。
                    if (File.Exists(dest))
                    {
                        Console.Error.WriteLine($"[LogArchiver] 同名のファイルが既にあるため移動しません: {dest}");
                        continue;
                    }

                    File.Move(file, dest);
                }
                catch (IOException ex)
                {
                    // **1ファイルの失敗で全体を止めない。** 書き込み中のログは
                    // ロックされていて移動できないが、それは次回の実行で片付く。
                    // ここで例外を投げると、後続のファイル・カテゴリが
                    // まるごと未処理のまま残る。
                    Console.Error.WriteLine($"[LogArchiver] 移動できませんでした（次回再試行します）: {file} - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.Error.WriteLine($"[LogArchiver] アクセスできませんでした: {file} - {ex.Message}");
                }
            }
        }

        if (purgeDays > 0)
        {
            // 隔離先のカテゴリは、出力先のそれと一致するとは限らない
            // （使わなくなったカテゴリの隔離済みログが残っている場合など）。
            // 隔離先の直下を改めて数える。
            var archivedCategories = Directory.Exists(archivePath)
                ? Directory.GetDirectories(archivePath).Select(Path.GetFileName).OfType<string>().ToArray()
                : Array.Empty<string>();
            PurgeExpired(archivePath, archivedCategories, DateTime.Now.AddDays(-purgeDays));
        }
    }

    /// <summary>
    /// 隔離先から、保存期間を過ぎたログを削除する。
    ///
    /// 隔離（File.Move）と同じく、1ファイルの失敗で全体を止めない。書き込み中・ロック中の
    /// ファイルは次回の実行で片付く。
    /// </summary>
    static void PurgeExpired(string archivePath, string[] categories, DateTime purgeThreshold)
    {
        foreach (var category in categories)
        {
            string dir = Path.Combine(archivePath, category);
            if (!Directory.Exists(dir))
                continue;

            foreach (var file in Directory.GetFiles(dir, "*.log"))
            {
                if (File.GetLastWriteTime(file) >= purgeThreshold)
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine($"[LogArchiver] 削除できませんでした（次回再試行します）: {file} - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.Error.WriteLine($"[LogArchiver] 削除できませんでした: {file} - {ex.Message}");
                }
            }
        }
    }
}
