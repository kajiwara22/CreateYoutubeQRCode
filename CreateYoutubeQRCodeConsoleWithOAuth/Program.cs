using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateYoutubeQRCodeConsoleWithOAuth
{
    class Program
    {
        static void Main(string[] args)
        {
            //(1)コマンドラインパーサーの初期化
            ParserSettings setting = new ParserSettings();
            setting.HelpWriter = Console.Error;
            var parser = new Parser(setting);

            //(2)オプション用クラスに引数を展開
            var option = new ConsoleOption();
            if (!parser.ParseArguments(args, option))
            {
                //パラメータに問題がある場合は、失敗ステータスで終了
                Environment.Exit(1);
            }
            var filePath = System.IO.Path.GetFullPath(option.OutputDirPath);


            var createYoutubeQRCode = new CreateYoutubeQRCodeLib.CreateYoutubeQRCode();
            createYoutubeQRCode.TargetContentType = CreateYoutubeQRCodeLib.contentType.User;
            createYoutubeQRCode.ConsumerKey = option.ClientID;
            createYoutubeQRCode.ConsumerSecret = option.ClientSeacret;
            createYoutubeQRCode.TargetFolderPath = filePath;

            var t = createYoutubeQRCode.CreateQR();
            t.Wait();
        }
    }
}
