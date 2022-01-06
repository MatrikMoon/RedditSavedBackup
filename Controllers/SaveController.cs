using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Reddit;
using Reddit.Inputs.Users;
using Reddit.Things;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedditSavedBackup.Controllers
{
    [Route("[controller]")]
    public class SaveController : Controller
    {
        private static readonly List<string> ImageExtensions = new() { ".jpg", ".jpe", ".bmp", ".gif", ".png" };

        [HttpGet("signin")]
        public IActionResult Login()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/save" }, "Reddit");
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Content("Loading your saved posts (this could take a minute)..." +
                "<meta http-equiv=\"refresh\" content=\"0;url=/save/saved\"/>", "text/html");
        }

        [HttpGet("saved")]
        public IActionResult Saved()
        {
            var accessToken = HttpContext.User.Claims.First(x => x.Type == "access_token").Value;
            var refreshToken = HttpContext.User.Claims.First(x => x.Type == "refresh_token").Value;

            RedditClient reddit = new(appId: Program.ClientID, appSecret: Program.ClientSecret, accessToken: accessToken, refreshToken: refreshToken);

            var accumulatedSavedPosts = new List<Post>();
            var after = string.Empty;
            var downloadIterations = 0;

            Console.WriteLine($"Downloading chunk {++downloadIterations}");
            var savedPosts = reddit.Models.Users.GetUser<PostContainer>(reddit.Account.Me.Name, "saved", new UsersHistoryInput(limit: 100)); after = savedPosts.Data.after;
            after = savedPosts.Data.after;

            var data = savedPosts.Data.Children.Select(x => x.Data).ToList();
            accumulatedSavedPosts.AddRange(data);

            while (!string.IsNullOrEmpty(after))
            {
                Console.WriteLine($"Downloading chunk {++downloadIterations}");

                savedPosts = reddit.Models.Users.GetUser<PostContainer>(reddit.Account.Me.Name, "saved", new UsersHistoryInput(after: after, limit: 100));
                after = savedPosts.Data.after;

                data = savedPosts.Data.Children.Select(x => x.Data).ToList();
                accumulatedSavedPosts.AddRange(data);
            }

            Console.WriteLine();
            Console.WriteLine($"Downloaded {downloadIterations} chunks, ended up with {accumulatedSavedPosts.Count} items, dating back to {accumulatedSavedPosts.OrderBy(x => x.CreatedUTC).ElementAt(0).CreatedUTC}");

            Console.WriteLine();
            Console.WriteLine($"First item: {accumulatedSavedPosts.First().Title} ({accumulatedSavedPosts.First().URL})");
            Console.WriteLine($"Last item: {accumulatedSavedPosts.Last().Title} ({accumulatedSavedPosts.Last().URL})");

            Console.WriteLine();
            var writeToFile = "<style>img{max-height:40vh;}</style></br><h2>Saved Posts</h2><ol>";
            foreach (var item in accumulatedSavedPosts)
            {
                writeToFile += $"<li><a href=\"https://reddit.com{item.Permalink}\">{item.Title}</a></li>\n";
                if (ImageExtensions.Contains(item.URL[^4..^0].ToLower()))
                {
                    writeToFile += $"<img src=\"{item.URL}\" />\n";
                }
                Console.WriteLine($"{item.Title} ({item.URL})");
            }
            writeToFile += "</ol>";

            return Content(writeToFile, "text/plain");
        }
    }
}
