using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Tesseract;
using System.Diagnostics;
using System.Text;

namespace OCRWithPictureTaking
{
    internal static class NativeMethods //CONSOLE SETUP
    {
        [DllImport("kernel32.dll")]
        internal static extern Boolean AllocConsole();
    }
    static class Program
    {
        #region Win API Functions and Constants
        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll",
            EntryPoint = "AttachConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern UInt32 AttachConsole(UInt32 dwProcessId);

        [DllImport("kernel32.dll",
            EntryPoint = "CreateFileW",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateFileW(
              string lpFileName,
              UInt32 dwDesiredAccess,
              UInt32 dwShareMode,
              IntPtr lpSecurityAttributes,
              UInt32 dwCreationDisposition,
              UInt32 dwFlagsAndAttributes,
              IntPtr hTemplateFile
            );

        private const UInt32 GENERIC_WRITE = 0x40000000;
        private const UInt32 GENERIC_READ = 0x80000000;
        private const UInt32 FILE_SHARE_READ = 0x00000001;
        private const UInt32 FILE_SHARE_WRITE = 0x00000002;
        private const UInt32 OPEN_EXISTING = 0x00000003;
        private const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        private const UInt32 ERROR_ACCESS_DENIED = 5;

        private const UInt32 ATTACH_PARRENT = 0xFFFFFFFF;

        #endregion
        static void Main()
        {
            Initialize();

            TakePicture picture = new TakePicture();

            var testImagePath = @"depth.png";
            
            try
            {
                var logger = new FormattedConsoleLogger();
                var resultPrinter = new ResultPrinter(logger);
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(testImagePath))
                    {
                        using (logger.Begin("Process image"))
                        {
                            var i = 1;
                            using (var page = engine.Process(img))
                            {
                                var text = page.GetText();
                                logger.Log("Text: {0}", text);

                                if (File.Exists(@"output.txt"))
                                    File.Delete(@"output.txt");
                                using (var Stream = File.Open(@"output.txt", FileMode.OpenOrCreate))
                                {
                                    using (var writer = new BinaryWriter(Stream, Encoding.UTF8))
                                    {
                                        writer.Write(text.Where(c => char.IsDigit(c)).ToArray());
                                    }
                                    Stream.Close();
                                }
                                logger.Log("Mean confidence: {0}", page.GetMeanConfidence());

                                using (var iter = page.GetIterator())
                                {
                                    iter.Begin();
                                    do
                                    {
                                        if (i % 2 == 0)
                                        {
                                            using (logger.Begin("Line {0}", i))
                                            {
                                                do
                                                {
                                                    using (logger.Begin("Word Iteration"))
                                                    {
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                                        {
                                                            logger.Log("New block");
                                                        }
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Para))
                                                        {
                                                            logger.Log("New paragraph");
                                                        }
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                                        {
                                                            logger.Log("New line");
                                                        }
                                                        logger.Log("word: " + iter.GetText(PageIteratorLevel.Word));
                                                    }
                                                } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                                            }
                                        }
                                        i++;
                                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }



        private class ResultPrinter
        {
            readonly FormattedConsoleLogger logger;

            public ResultPrinter(FormattedConsoleLogger logger)
            {
                this.logger = logger;
            }

            public void Print(ResultIterator iter)
            {
                logger.Log("Is beginning of block: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Block));
                logger.Log("Is beginning of para: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Para));
                logger.Log("Is beginning of text line: {0}", iter.IsAtBeginningOf(PageIteratorLevel.TextLine));
                logger.Log("Is beginning of word: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Word));
                logger.Log("Is beginning of symbol: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Symbol));

                logger.Log("Block text: \"{0}\"", iter.GetText(PageIteratorLevel.Block));
                logger.Log("Para text: \"{0}\"", iter.GetText(PageIteratorLevel.Para));
                logger.Log("TextLine text: \"{0}\"", iter.GetText(PageIteratorLevel.TextLine));
                logger.Log("Word text: \"{0}\"", iter.GetText(PageIteratorLevel.Word));
                logger.Log("Symbol text: \"{0}\"", iter.GetText(PageIteratorLevel.Symbol));
            }
        }

        static public void Initialize(bool alwaysCreateNewConsole = false) //Sets up the Console Output
        {
            bool consoleAttached = true;
            if (alwaysCreateNewConsole
                || (AttachConsole(ATTACH_PARRENT) == 0
                && Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
            {
                consoleAttached = AllocConsole() != 0;
            }

            if (consoleAttached)
            {
                InitializeOutStream();
                InitializeInStream();
            }
        }

        private static void InitializeOutStream() //CONSOLE SETUP
        {
            var fs = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);
            if (fs != null)
            {
                var writer = new StreamWriter(fs) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }
        }

        private static void InitializeInStream() //CONSOLE SETUP
        {
            var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);
            if (fs != null)
            {
                Console.SetIn(new StreamReader(fs));
            }
        }

        private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode, //CONSOLE SETUP
                                FileAccess dotNetFileAccess)
        {
            var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);
            if (!file.IsInvalid)
            {
                var fs = new FileStream(file, dotNetFileAccess);
                return fs;
            }
            return null;
        }
    }
}
