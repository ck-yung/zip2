using System.Collections.Immutable;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2.create
{
    internal class Command : CommandBase
    {
        static bool IsValidZipFileSize(long size) => size >= 100;

        public override int Invoke()
        {
            try
            {
                switch (string.IsNullOrEmpty(Password),
                    string.IsNullOrEmpty(PasswordFrom),
                    string.IsNullOrEmpty(PasswordFromRaw))
                {
                    case (true, false, true):
                        using (var inpFs = File.OpenText(PasswordFrom))
                        {
                            var textThe = inpFs.ReadToEnd().Trim();
                            if (string.IsNullOrEmpty(textThe))
                            {
                                TotalPrintLine(
                                    $"File '{PasswordFrom}' only contains blank content!");
                                return 1;
                            }
                            ((IParser)Password).Parse(textThe);
                        }
                        break;
                    case (true, true, false):
                        using (var inpFs = File.OpenRead(PasswordFromRaw))
                        {
                            var readSize = new FileInfo(PasswordFromRaw).Length;
                            if (1 > readSize)
                            {
                                TotalPrintLine($"File '{PasswordFromRaw}' is empty!");
                                return 1;
                            }
                            var buf = new byte[readSize];
                            inpFs.Read(buf);
                            var textThe = Encoding.UTF8.GetString(buf);
                            ((IParser)Password).Parse(textThe);
                        }
                        break;
                    case (false, false, _):
                    case (false, _, false):
                    case (_, false, false):
                        TotalPrintLine(
                            " Only one of '--password', '--password-from' and '--password-from-raw' can be assigned.");
                        return 1;
                    default:
                        break;
                }

                switch (FilenamesToBeBackup.Count(),
                    string.IsNullOrEmpty(FilesFrom))
                {
                    case (0, true):
                        TotalPrintLine("No file to be backup.");
                        return 1;
                    case ( > 0, false):
                        Console.Write("Cannot handle files from --files-from");
                        Console.WriteLine($"={FilesFrom} and command-line arg FILE.");
                        return 1;
                    case (0, false):
                        if (FilesFrom == "-")
                        {
                            if (!Console.IsInputRedirected)
                            {
                                Console.WriteLine("Only support redir input.");
                                return 1;
                            }
                            FilenamesToBeBackup.AddRange(
                                Helper.ReadConsoleAllLines()
                                .Select((it) => it.Trim())
                                .Where((it) => it.Length > 0)
                                .Select((it) => Helper.ToStandardDirSep(it))
                                .Distinct());
                        }
                        else
                        {
                            FilenamesToBeBackup.AddRange(File
                                .ReadAllLines(FilesFrom)
                                .Select((it) => it.Trim())
                                .Where((it) => it.Length > 0)
                                .Distinct());
                        }

                        if (FilenamesToBeBackup.Count() == 0)
                        {
                            TotalPrintLine(
                                $"No file is found in (files-from) '{FilesFrom}'");
                            return 1;
                        }
                        break;
                    default:
                        break;
                }

                if (File.Exists(zipFilename))
                {
                    Console.WriteLine($"Output zip file '{zipFilename}' is found!");
                    return 1;
                }

                var zDirThe = Path.GetDirectoryName(zipFilename);
                var zFilename = Path.GetFileName(zipFilename);
                var zShadowOutputFilename = zFilename + "."
                    + Path.GetRandomFileName() + ".tmp";
                var zShadowOutputPathName = (string.IsNullOrEmpty(zDirThe))
                    ? zShadowOutputFilename
                    : Path.Join(zDirThe, zShadowOutputFilename);

                (int countAddedIntoZip, int countMovedToArchivedDir) =
                    InvokeAddToZipFucntion( zipFilename,
                        zShadowOutputPathName);

                var zShadowOutputFileSize =
                    File.Exists(zShadowOutputPathName)
                    ? new FileInfo(zShadowOutputPathName).Length
                    : -1;

                switch (countAddedIntoZip, countMovedToArchivedDir,
                    IsValidZipFileSize(zShadowOutputFileSize))
                {
                    case (0, _, _):
                        TotalPrintLine(" No file is added into zip file.");
                        if (File.Exists(zShadowOutputPathName))
                        {
                            File.Delete(zShadowOutputPathName);
                        }
                        break;
                    case (1, 0, true):
                        TotalPrintLine(" One file is added into zip file.");
                        break;
                    case (1, 1, true):
                        TotalPrintLine(" One file is added into and moved to archived dir.");
                        break;
                    case ( > 1, 0, true):
                        TotalPrintLine($" {countAddedIntoZip} files are added into zip file.");
                        break;
                    case ( > 1, > 0, true):
                        TotalPrintLine($" {countAddedIntoZip} files are added into zip file, {countMovedToArchivedDir} are moved to archived dir.");
                        break;
                    default:
                        TotalPrintLine(" Unknown error");
                        if (File.Exists(zShadowOutputPathName))
                        {
                            File.Delete(zShadowOutputPathName);
                        }
                        break;
                }

                var temp2Filename = Path.GetRandomFileName() + ".tmp";
                var temp2Pathname = (string.IsNullOrEmpty(zDirThe))
                    ? temp2Filename : Path.Join(zDirThe, temp2Filename);

                switch (File.Exists(zipFilename),
                    File.Exists(zShadowOutputPathName))
                {
                    case (false, true):
                        try
                        {
                            new FileInfo(zShadowOutputPathName)
                                .MoveTo(zipFilename);
                        }
                        catch (Exception ee)
                        {
                            Console.Error.WriteLine($"{ee.Message}");
                            Console.Error.WriteLine(
                                $"Failed to rename to '{zipFilename}'");
                        }
                        break;

                    case (true, true):
                        try
                        {
                            new FileInfo(zipFilename)
                                .MoveTo(temp2Pathname);
                            new FileInfo(zShadowOutputPathName)
                                .MoveTo(zipFilename);
                        }
                        catch (Exception ee)
                        {
                            Console.Error.WriteLine($"{ee.Message}");
                            Console.Error.WriteLine(
                                $" Failed while rename file '{zipFilename}'");
                        }
                        finally
                        {
                            if (File.Exists(temp2Pathname))
                            {
                                File.Delete(temp2Pathname);
                            }
                        }
                        break;

                    case (true, false):
                        if (File.Exists(zipFilename))
                        {
                            File.Delete(zipFilename);
                        }
                        break;

                    default:
                        Console.Error.WriteLine(
                            " Failed by some unknown error!");
                        break;
                }

                return 0;
            }
            finally
            {
                if (File.Exists(zipFilename) &&
                    new FileInfo(zipFilename).Length < 10)
                {
                    File.Delete(zipFilename);
                }
            }
        }

        static bool AddToZip(string filename, ZipOutputStream zs)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    ItemPrint(" not found");
                    return false;
                }

                var sizeThe = (new FileInfo(filename)).Length;
                var entry = new ZipEntry(Helper.ToStandardDirSep(filename))
                {
                    DateTime = File.GetLastWriteTime(filename),
                    Size = sizeThe,
                };

                long writtenSize = 0L;
                zs.PutNextEntry(entry, isTranscational:true);
                byte[] buffer = new byte[32 * 1024];
                using (FileStream fs = File.OpenRead(filename))
                {
                    try
                    {
                        for (int readSize = fs.Read(buffer, 0, buffer.Length);
                            readSize > 0 && sizeThe > writtenSize;
                            readSize = fs.Read(buffer, 0, buffer.Length))
                        {
                            zs.Write(buffer, 0, readSize);
                            writtenSize += readSize;
                        }
                    }
                    catch (ZipException zipEe)
                    {
                        ItemErrorPrintFilename(filename);
                        ItemErrorPrintMessage($" {zipEe.Message}");
                    }
                    catch (Exception ee)
                    {
                        ItemErrorPrintFilename(filename);
                        var checkDebug = Environment
                        .GetEnvironmentVariable("zip2");
                        if (checkDebug?.Contains(":debug:") ?? false)
                        {
                            ItemErrorPrintMessage($" {ee.ToString()}");
                        }
                        else
                        {
                            ItemErrorPrintMessage($" {ee.Message}");
                        }
                    }
                }

                if (sizeThe==writtenSize)
                {
                    zs.CloseEntry();
                }
                else
                {
                    ItemErrorPrintFilename(filename);
                    ItemErrorPrintMessage(" is abortd");
                    ItemPrint(
                        $" because want {sizeThe}b but find {writtenSize}b !");
                    zs.Rollback();
                    return false;
                }

                return true;
            }
            catch (Exception ee)
            {
                ItemErrorPrintFilename(filename);
                ItemErrorPrintMessage($" {ee.Message}");
                return false;
            }
        }

        static (int, int) InvokeAddToZip(
            string zipFilename,
            string shadowOutputPathName)
        {
            int countAddedIntoZip = 0;
            int countMovedToArchivedDir = 0;

            using (var realOutputFile = File.Create(zipFilename))
            {
                realOutputFile.Write(new byte[] { 19, 97, 7, 1 });
                using (ZipOutputStream zs = new ZipOutputStream(
                    File.Create(shadowOutputPathName)))
                {
                    zs.UseZip64 = UseZip64.Dynamic;
                    zs.SetLevel(CompressLevel);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        zs.Password = Password;
                    }

                    foreach (var filename in FilenamesToBeBackup)
                    {
                        ItemPrint(filename);

                        bool addResult = AddToZip( filename, zs);

                        ItemPrint(Environment.NewLine);
                        if (!addResult) continue;

                        countAddedIntoZip += 1;
                        countMovedToArchivedDir += MoveToArchivedDir(filename)
                            ? 1 : 0;
                    }

                    zs.Finish();
                    zs.Close();
                }
            }
            return (countAddedIntoZip, countMovedToArchivedDir);
        }

        static ImmutableDictionary<string, string[]> SwitchShortCuts =
            new Dictionary<string, string[]>
            {
                [QuietShortcut] = new string[] { QuietText },
                ["-0"] = new string[] { "--compress-level=store" },
                ["-1"] = new string[] { "--compress-level=fast" },
                ["-2"] = new string[] { "--compress-level=good" },
                ["-3"] = new string[] { "--compress-level=better" },
                ["-m"] = new string[] { "--move-after-archived" },
            }.ToImmutableDictionary<string, string[]>();

        static ImmutableDictionary<string, string> OptionShortCuts =
            new Dictionary<string, string>
            {
                ["-T"] = FilesFromPrefix,
            }.ToImmutableDictionary<string, string>();

        static List<string> FilenamesToBeBackup = new List<string>();

        public override bool Parse(IEnumerable<string> args)
        {
            (string[] optUnknown, string[] filenamesToBeBackup) =
                opts.ParseFrom(
                Helper.ExpandToOptions(args,
                switchShortcuts: SwitchShortCuts,
                optionShortcuts: OptionShortCuts));

            FilenamesToBeBackup.AddRange(
                filenamesToBeBackup
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct());

            return true;
        }

        public override int SayHelp()
        {
            return SayHelp(nameof(create), opts
                , OptionShortCuts
                , SwitchShortCuts
                , zipFileHint:"NEWZIPFILE"
                );
        }

        static readonly ParameterOption<int> CompressLevel
            = new ParameterOptionSetter<int>("compress-level",
                "store|fast|good|better  (default good)", 5,
                parse: (val,obj) =>
                {
                    switch (val)
                    {
                        case "store":
                            obj.SetValue(0);
                            return true;
                        case "fast":
                            obj.SetValue(2);
                            return true;
                        case "good":
                            obj.SetValue(5);
                            return true;
                        case "better":
                            obj.SetValue(9);
                            return true;
                        default:
                            return false;
                    }
                });

        static public Func<string,bool> MoveToArchivedDir
        { get; private set; } = (_) => false;

        static readonly ParameterSwitch MoveToArchivedDirSwitch =
        new ParameterSwitch("move-after-archived",
        help: "move archived files to \"zip2archived <TIMESTAMP>\"",
        whenSwitch: () =>
        {
            var dirMoveTo = "zip2archived "
                + DateTime.Now.ToString("s").Replace(":", "-")
                + "." + DateTime.Now.ToString("fff");
            MoveToArchivedDir = (fname) =>
            {
                if (string.IsNullOrEmpty(fname) ||
                !File.Exists(fname)) return false;
                try
                {
                    var destPathname = Path.Join(dirMoveTo, fname);
                    var destDir = Path.GetDirectoryName(destPathname);
                    if (!string.IsNullOrEmpty(destDir) &&
                    !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    new FileInfo(fname).MoveTo(destPathname);
                    return true;
                }
                catch (Exception ee)
                {
                    Console.Error.WriteLine(
                        $"Failed to move '{fname}' to '{dirMoveTo}':{ee.Message}");
                    return false;
                }
            };
        });

        static public Func<string, string, (int,int)> InvokeAddToZipFucntion
        { get; private set; } = InvokeAddToZip;

        static IParser[] opts =
        {
            Quiet,
            TotalOff,
            FilesFrom,
            CompressLevel,
            MoveToArchivedDirSwitch,
            Password,
            PasswordFrom,
            PasswordFromRaw,
        };
    }
}
