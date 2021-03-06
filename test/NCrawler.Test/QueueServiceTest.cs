using System;

using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NCrawler.HtmlProcessor;
using NCrawler.Interfaces;
using NCrawler.Services;
using NCrawler.Test.Helpers;

using NUnit.Framework;
using System.Threading.Tasks;

namespace NCrawler.Test
{
	[TestFixture]
	public class QueueServiceTest
	{
		public void Test1(ICrawlerQueue crawlQueue)
		{
			Assert.NotNull(crawlQueue);
			Assert.AreEqual(0, crawlQueue.Count);

			if(crawlQueue is IDisposable)
			{
				((IDisposable)crawlQueue).Dispose();
			}
		}

		public void Test2(ICrawlerQueue crawlQueue)
		{
			Assert.NotNull(crawlQueue);
			crawlQueue.Push(new CrawlerQueueEntry());
			Assert.AreEqual(1, crawlQueue.Count);

			if (crawlQueue is IDisposable)
			{
				((IDisposable)crawlQueue).Dispose();
			}
		}

		public void Test3(ICrawlerQueue crawlQueue)
		{
			Assert.NotNull(crawlQueue);
			crawlQueue.Push(new CrawlerQueueEntry());
			crawlQueue.Pop();
			Assert.AreEqual(0, crawlQueue.Count);

			if (crawlQueue is IDisposable)
			{
				((IDisposable)crawlQueue).Dispose();
			}
		}

		public void Test4(ICrawlerQueue crawlQueue)
		{
			Assert.NotNull(crawlQueue);
			crawlQueue.Push(new CrawlerQueueEntry());
			crawlQueue.Pop();
			Assert.AreEqual(0, crawlQueue.Count);
			var actualValue = crawlQueue.Pop();
			Assert.IsNull(actualValue);

			if (crawlQueue is IDisposable)
			{
				((IDisposable)crawlQueue).Dispose();
			}
		}

		public void Test5(ICrawlerQueue crawlQueue)
		{
			Assert.NotNull(crawlQueue);
			DateTime now = DateTime.Now;
			crawlQueue.Push(new CrawlerQueueEntry
				{
					CrawlStep = new CrawlStep(new Uri("http://www.biz.org/"), 0),
					Properties = new Dictionary<string, object>
						{
							{"one", "string"},
							{"two", 123},
							{"three", now},
						},
					Referrer = new CrawlStep(new Uri("http://www.biz3.org/"), 1)
				});
			Assert.AreEqual(1, crawlQueue.Count);
			CrawlerQueueEntry entry = crawlQueue.Pop();
			Assert.AreEqual(0, crawlQueue.Count);
			Assert.NotNull(entry);
			Assert.NotNull(entry.CrawlStep);
			Assert.NotNull(entry.Properties);
			Assert.NotNull(entry.Referrer);
			Assert.AreEqual(0, entry.CrawlStep.Depth);
			Assert.AreEqual("http://www.biz.org/", entry.CrawlStep.Uri.ToString());
			Assert.AreEqual("one", entry.Properties.Keys.First());
			Assert.AreEqual("two", entry.Properties.Keys.Skip(1).First());
			Assert.AreEqual("three", entry.Properties.Keys.Skip(2).First());
			Assert.AreEqual("string", entry.Properties["one"]);
			Assert.AreEqual(123, entry.Properties["two"]);
			Assert.AreEqual(now, entry.Properties["three"]);
			Assert.AreEqual(0, crawlQueue.Count);

			if (crawlQueue is IDisposable)
			{
				((IDisposable)crawlQueue).Dispose();
			}
		}

		public void RunCrawlerQueueTests(Func<ICrawlerQueue> constructor)
		{
			Test1(constructor());
			Test2(constructor());
			Test3(constructor());
			Test4(constructor());
			Test5(constructor());
		}

		[Test]
		public void TestQueueServiceServices()
		{
			RunCrawlerQueueTests(() => new InMemoryCrawlerQueueService());
		}

		private static CollectorStep CollectionCrawl()
		{
			CollectorStep collectorStep = new CollectorStep();
			HtmlDocumentProcessor htmlDocumentProcessor = new HtmlDocumentProcessor();
			using (Crawler crawler = new Crawler(new Uri("http://ncrawler.codeplex.com"), collectorStep, htmlDocumentProcessor))
			{
				Console.Out.WriteLine(crawler.GetType());
				crawler.MaximumThreadCount = 5;
				crawler.UriSensitivity = UriComponents.HttpRequestUrl;
				crawler.ExcludeFilter = new[]
					{
						new RegexFilter(
							new Regex(@"(\.jpg|\.css|\.js|\.gif|\.jpeg|\.png)",
								RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
					};
				crawler.CrawlAsync().Wait();
				return collectorStep;
			}
		}

		[Test]
		public void TestQueuesYieldSameResult()
		{
			TestModule.SetupInMemoryStorage();
			CollectorStep reference = CollectionCrawl();
			CollectorStep inMemoryCrawlerCollectorStep = CollectionCrawl();
			Assert.AreEqual(reference.Steps.Count, inMemoryCrawlerCollectorStep.Steps.Count);
		}
	}

	internal class CollectorStep : IPipelineStep
	{
		private readonly List<CrawlStep> m_Steps = new List<CrawlStep>();

		public List<CrawlStep> Steps
		{
			get{ return m_Steps; }
		}

		public Task ProcessAsync(Crawler crawler, PropertyBag propertyBag)
		{
			Steps.Add(propertyBag.Step);
            return Task.CompletedTask;
		}
	}
}