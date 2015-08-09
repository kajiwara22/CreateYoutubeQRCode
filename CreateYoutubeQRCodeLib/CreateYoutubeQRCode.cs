using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using System.IO;
using System.Windows;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Diagnostics;
using AsyncOAuth;
using System.Security.Cryptography;
using System.Net;

namespace CreateYoutubeQRCodeLib
{
    public class CreateYoutubeQRCode
    {
         /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public String UserName { get; set; }
        public String UserChannnel { get; set; }
        public String APIKey { get; set; }
        public String ConsumerKey { get; set; }
        public String ConsumerSecret { get; set; }
        public String TargetFolderPath { get; set; }
        public contentType TargetContentType {get;set;}
        private int counter = 0;

        public async Task CreateQR()
        {
            var jsonString = string.Empty;
            var access_token = string.Empty;
            if (!String.IsNullOrEmpty(ConsumerKey) && !String.IsNullOrEmpty(ConsumerSecret))
            {
                var tokenFile = @"./token.json";
                if (!File.Exists(tokenFile))
                {
                    var accessTokenResponse = await getAccessToken();
                    jsonString = await accessTokenResponse.Content.ReadAsStringAsync();
                    var tokenJson = Codeplex.Data.DynamicJson.Parse(jsonString);
                    StreamWriter writer = new StreamWriter(tokenFile, false);
                    writer.WriteLine(jsonString);
                    writer.Close();
                    access_token = tokenJson.access_token;
                }
                else
                {
                    StreamReader sr = new StreamReader(tokenFile);
                    string text = sr.ReadToEnd();
                    sr.Close();
                    var tokenJson = Codeplex.Data.DynamicJson.Parse(text);
                    var refresh_token = tokenJson.refresh_token;
                    var refreshTokenResponse = await refreshAccessToken(refresh_token);
                    jsonString = await refreshTokenResponse.Content.ReadAsStringAsync();
                    tokenJson = Codeplex.Data.DynamicJson.Parse(jsonString);
                    access_token = tokenJson.access_token;

                    tokenJson.access_token = access_token;

                    var reJson = tokenJson.ToString();
                    if (String.IsNullOrEmpty(reJson))
                    {
                        StreamWriter writer = new StreamWriter(tokenFile, false);
                        writer.WriteLine(tokenJson.toString());
                        writer.Close();
                    }

                }
            }
            
            
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var youtubeMovieUrlTemplate = "https://www.youtube.com/watch?v=";
            // GetWebPageAsyncメソッドを呼び出す
            Task<string> webTask;
            var uploadPlaylistId = String.Empty;
            if (String.IsNullOrEmpty(access_token))
            {
                webTask = getYoutubeItemList(String.Empty);
                jsonString = await webTask;
                var json = Codeplex.Data.DynamicJson.Parse(jsonString);
                var check = json.items.Deserialize<Object[]>();
                if ((check as Array).Length == 0)
                {
                    this.TargetContentType = contentType.Channel;
                    this.UserChannnel = this.UserName;
                    webTask = getYoutubeItemList(String.Empty);
                    jsonString = await webTask;
                    json = Codeplex.Data.DynamicJson.Parse(jsonString);
                    check = json.items.Deserialize<Object[]>();
                    if ((check as Array).Length == 0)
                    {
                        return;
                    }
                }
                uploadPlaylistId = json.items[0].contentDetails.relatedPlaylists.uploads;
            }
            
            
            
            
            var nextPageToken = String.Empty;
            var hasNextPage = true;
            while (hasNextPage)
            {
                var jsonString2 = String.Empty;
                if (String.IsNullOrEmpty(access_token))
                {
                    webTask = getYoutubeMovieItemList(uploadPlaylistId, nextPageToken,String.Empty);
                }
                else
                {
                    webTask = getYoutubeMovieItemList(uploadPlaylistId, nextPageToken,access_token);
                }
                
                jsonString2 = await webTask;
                var json2 = Codeplex.Data.DynamicJson.Parse(jsonString2);
                foreach (var item in json2.items)
                {
                    Debug.WriteLine(counter.ToString());
                    var fileName = item.snippet.title as String;
                    char[] invalidch = Path.GetInvalidFileNameChars();
                    foreach (char c in invalidch)
                    {
                        fileName = fileName.Replace(c, '_');
                    }
                    fileName = fileName.Replace(" ", "_") + ".png";
                    var url = String.Empty;
                    if (String.IsNullOrEmpty(access_token))
                    {
                         url = youtubeMovieUrlTemplate + item.snippet.resourceId.videoId;
                    }
                    else
                    {
                         url = youtubeMovieUrlTemplate + item.id.videoId;
                         
                    }
                    writeQR(url, TargetFolderPath + "\\" + counter + "_" + fileName);
                    
                    
                    counter++;
                }
                if (json2.IsDefined("nextPageToken"))
                {
                    nextPageToken = json2.nextPageToken;
                    hasNextPage = !String.IsNullOrEmpty(nextPageToken);
                    Debug.WriteLine("has next Page");
                }
                else
                {
                    hasNextPage = false;
                    Debug.WriteLine("has next no page");
                }
            }
            
        }


        private async Task<string> getYoutubeItemList(String accessToken)
        {
            using (HttpClient client = new HttpClient())
            {
                // タイムアウトをセット（オプション）
                client.Timeout = TimeSpan.FromSeconds(60.0);

                try
                {
                    Uri uri;
                    if (String.IsNullOrEmpty(accessToken))
                    {
                        uri =  new Uri(getURL());
                    }else{
                        uri =  new Uri(getURL(accessToken));
                    }
                    
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

        private async Task<HttpResponseMessage> getAccessToken()
        {
           
            var redirectUrl = "http://localhost:18056";
            var getAccessTokenBaseUrl = "https://accounts.google.com/o/oauth2/auth";
            var getAccessTokenUrl = getAccessTokenBaseUrl + "?client_id=" + ConsumerKey + "&redirect_uri="+ redirectUrl +
                "&response_type=code&scope=https://www.googleapis.com/auth/youtubepartner-channel-audit https://www.googleapis.com/auth/youtube.readonly";
            Uri uri = new Uri(getAccessTokenUrl);
            Process.Start(getAccessTokenUrl);

            var pinCode = String.Empty;
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(redirectUrl + "/");
            listener.Start();
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest req = context.Request;
                HttpListenerResponse res = context.Response;

                Console.WriteLine(req.RawUrl);

                if (File.Exists("./index.html")) {
                    byte[] content = File.ReadAllBytes("./index.html");
                    res.ContentType = "text/html";
                    res.OutputStream.Write(content, 0, content.Length);
                }
                
                if (req.QueryString.Count > 0)
                {
                    pinCode = req.QueryString["code"];
                    res.Close();
                    break;
                }
                else
                {
                    res.Close();
                }
            }

            // enter pin
            using (HttpClient client = new HttpClient())
            {
                // タイムアウトをセット（オプション）
                client.Timeout = TimeSpan.FromSeconds(60.0);
                if (!string.IsNullOrEmpty(pinCode))
                {
                    var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "code", pinCode },
                        { "client_id", ConsumerKey },
                        { "client_secret", ConsumerSecret },
                        { "redirect_uri", redirectUrl },
                        { "grant_type", "authorization_code"  }
                    });
                    return await client.PostAsync("https://accounts.google.com/o/oauth2/token", content);
                }
                else
                {
                    return null;
                }

            }

        }

        private async Task<HttpResponseMessage> refreshAccessToken(String refreshToken)
        {
           
            using (HttpClient client = new HttpClient())
            {
                // タイムアウトをセット（オプション）
                client.Timeout = TimeSpan.FromSeconds(60.0);

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ConsumerKey },
                    { "client_secret", ConsumerSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token"  }
                });
                return await client.PostAsync("https://accounts.google.com/o/oauth2/token", content);
            }                
        }

        private async Task<string> getYoutubeMovieItemList(String uploadPlaylistId,String nextPageToken,String accessToken)
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
                    String itemListURL;
                    if (String.IsNullOrEmpty(accessToken))
                    {
                        itemListURL = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&playlistId=" + uploadPlaylistId + "&maxResults=50" + nextPagePhrase + "&key=" + APIKey;
                    }
                    else
                    {
                        itemListURL = "https://www.googleapis.com/youtube/v3/search?part=snippet%2Cid&forMine=true&type=video&maxResults=50" + nextPagePhrase + "&access_token=" + accessToken;
                    }
                    Uri uri = new Uri(itemListURL);
                    Debug.WriteLine(itemListURL);
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

        private String getURL(String accessToken)
        {
            String returnUrl = String.Empty;
            
                    returnUrl = "https://www.googleapis.com/youtube/v3/channels?part=id&mine=true&access_token=" + accessToken;
            return returnUrl;

        }

    }
    public enum contentType
    {
        User,
        Channel
    }
}
