using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using System.IO;
using System.Windows;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Net.Http;

namespace CreateYoutubeQRCode.Models
{
    public class Model : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public String UserName { get; set; }
        public String UserChannnel { get; set; }
        public String APIKey { get; set; }
        public String TargetFolderPath { get; set; }
        public contentType TargetContentType {get;set;}

        public async void CreateQR()
        {
            var youtubeMovieUrlTemplate = "https://www.youtube.com/watch?v=";
            // GetWebPageAsyncメソッドを呼び出す
            Task<string> webTask = getYoutubeItemList();
            String jsonString = await webTask;
            var json = Codeplex.Data.DynamicJson.Parse(jsonString);
            var tempitems = json.items[0];
            var uploadPlaylistId = tempitems.uploads;
            var nextPageToken = String.Empty;
            var hasNextPage = true;
            while (hasNextPage)
            {
                webTask = getYoutubeMovieItemList(uploadPlaylistId, nextPageToken);
                jsonString = await webTask;
                json = Codeplex.Data.DynamicJson.Parse(jsonString);
                foreach (var item in json.items)
                {
                    var fileName = item.snippet.title as String;
                    char[] invalidch = Path.GetInvalidFileNameChars();

                    foreach (char c in invalidch)
                    {
                        fileName = fileName.Replace(c, '_');
                    }
                    var url = youtubeMovieUrlTemplate + item.resourceId.videoId;
                    writeQR(url, TargetFolderPath + "\\" + fileName);
                }
                if(json.nextPageToken != null){
                    nextPageToken = json.nextPageToken;
                }
                else
                {
                    hasNextPage = false;
                }
            }
            
        }


        private async Task<string> getYoutubeItemList()
        {
            using (HttpClient client = new HttpClient())
            {
                // タイムアウトをセット（オプション）
                client.Timeout = TimeSpan.FromSeconds(60.0);

                try
                {
                    Uri uri = new Uri(getURL());
                    // Webページを取得するのは、事実上この1行だけ
                    return await client.GetStringAsync(uri);
                }
                catch (HttpRequestException e)
                {
                    // 404エラーや、名前解決失敗など
                    Console.WriteLine("\n例外発生!");
                    // InnerExceptionも含めて、再帰的に例外メッセージを表示する
                    Exception ex = e;
                    while (ex != null)
                    {
                        Console.WriteLine("例外メッセージ: {0} ", ex.Message);
                        ex = ex.InnerException;
                    }
                }
                catch (TaskCanceledException e)
                {
                    // タスクがキャンセルされたとき（一般的にタイムアウト）
                    Console.WriteLine("\nタイムアウト!");
                    Console.WriteLine("例外メッセージ: {0} ", e.Message);
                }
                return null;
            }
        }

        private async Task<string> getYoutubeMovieItemList(String uploadPlaylistId,String nextPageToken)
        {
            using (HttpClient client = new HttpClient())
            {
                // タイムアウトをセット（オプション）
                client.Timeout = TimeSpan.FromSeconds(60.0);

                try
                {
                    String nextPagePhrase = String.Empty;
                    if (!String.IsNullOrEmpty(nextPageToken))
                    {
                        nextPagePhrase = "&pageToken=" +  nextPageToken;
                    }
                    var itemListURL = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&playlistId=" + uploadPlaylistId + "&maxResults=50" + nextPagePhrase + "&key=" + APIKey;
                    Uri uri = new Uri(getURL());
                    // Webページを取得するのは、事実上この1行だけ
                    return await client.GetStringAsync(uri);
                }
                catch (HttpRequestException e)
                {
                    // 404エラーや、名前解決失敗など
                    Console.WriteLine("\n例外発生!");
                    // InnerExceptionも含めて、再帰的に例外メッセージを表示する
                    Exception ex = e;
                    while (ex != null)
                    {
                        Console.WriteLine("例外メッセージ: {0} ", ex.Message);
                        ex = ex.InnerException;
                    }
                }
                catch (TaskCanceledException e)
                {
                    // タスクがキャンセルされたとき（一般的にタイムアウト）
                    Console.WriteLine("\nタイムアウト!");
                    Console.WriteLine("例外メッセージ: {0} ", e.Message);
                }
                return null;
            }
        }

        private void writeQR(String url ,String fileName)
        {
            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.M);
            QrCode qrCode = qrEncoder.Encode(url);
            GraphicsRenderer gRender = new GraphicsRenderer(new FixedModuleSize(30, QuietZoneModules.Four));
            BitMatrix matrix = qrCode.Matrix;
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                gRender.WriteToStream(matrix, ImageFormat.Png, stream, new System.Drawing.Point(600, 600));
            }
        }

        private String getURL()
        {
            String returnUrl = String.Empty;
            switch (TargetContentType)
            {
                case contentType.User:
                    returnUrl = "https://www.googleapis.com/youtube/v3/channels?part=contentDetails&forUsername=" + UserName + "&key=" + APIKey;
                    break;
                case contentType.Channel:
                    returnUrl = "https://www.googleapis.com/youtube/v3/channels?part=contentDetails&id=" + UserChannnel + "&key=" + APIKey;
                    break;
                default:
                    break;
            }
            return returnUrl;
            
        }

    }
    public enum contentType
    {
        User,
        Channel
    }

}

