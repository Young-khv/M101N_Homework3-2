using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using M101DotNet.WebApp.Models;
using M101DotNet.WebApp.Models.Home;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace M101DotNet.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var blogContext = new BlogContext();
           
            var recentPosts = await blogContext.Posts.Find(new BsonDocument())
                                                     .SortByDescending(t=>t.CreatedAtUtc)
                                                     .Limit(10)
                                                     .ToListAsync();

            var model = new IndexModel
            {
                RecentPosts = recentPosts
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult NewPost()
        {
            return View(new NewPostModel());
        }

        [HttpPost]
        public async Task<ActionResult> NewPost(NewPostModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var blogContext = new BlogContext();
            var post = new Post()
            {
                Author = this.User.Identity.Name,
                Title = model.Title,
                Content = model.Content,
                CreatedAtUtc = DateTimeOffset.Now,
                Comments = new List<Comment>(),
                Tags = model.Tags.Split(new []{ ',', '\n', ' ',';' }).ToList()
            };
            await blogContext.Posts.InsertOneAsync(post);
            return RedirectToAction("Post", new { id = post.Id });
        }

        [HttpGet]
        public async Task<ActionResult> Post(string id)
        {
            var blogContext = new BlogContext();

            var post = await blogContext.Posts.Find(t => t.Id == id)
                                        .FirstAsync();

            if (post == null)
            {
                return RedirectToAction("Index");
            }

            var model = new PostModel
            {
                Post = post
            };

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Posts(string tag = null)
        {
            var blogContext = new BlogContext();

            var posts = await blogContext.Posts.Find(t => t.Tags.Contains(tag))
                                               .SortByDescending(t => t.CreatedAtUtc)
                                               .ToListAsync();

            if(posts.Count == 0)
                posts = await blogContext.Posts.Find(new BsonDocument())
                                                     .SortByDescending(t => t.CreatedAtUtc)
                                                     .ToListAsync();

            return View(posts);
        }

        [HttpPost]
        public async Task<ActionResult> NewComment(NewCommentModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Post", new { id = model.PostId });
            }

            var blogContext = new BlogContext();
            
            var comment = new Comment()
            {
                Author = this.User.Identity.Name,
                Content = model.Content,
                CreatedAtUtc = DateTimeOffset.Now
            };

            await blogContext.Posts.UpdateOneAsync(
                  Builders<Post>.Filter.Eq(t=>t.Id, model.PostId),
                  Builders<Post>.Update.Push(t => t.Comments, comment));

            return RedirectToAction("Post", new { id = model.PostId });
        }
    }
}