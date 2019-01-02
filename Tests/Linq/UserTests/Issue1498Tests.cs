﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1498Tests : TestBase
	{
		public class Topic
		{
			public int Id { get; set; }

			public string Title { get; set; }

			public string Text { get; set; }

			[Association(ThisKey = "Id", OtherKey = "TopicId")]
			public virtual ICollection<Message> MessagesA1 { get; set; }

			[Association(ExpressionPredicate = nameof(Predicate))]
			public virtual ICollection<Message> MessagesA2 { get; set; }

			[Association(QueryExpressionMethod = nameof(Query))]
			public virtual ICollection<Message> MessagesA3 { get; set; }

			public virtual ICollection<Message> MessagesF { get; set; }

			static Expression<Func<Topic, Message, bool>> Predicate => (t, m) => t.Id == m.TopicId;

			static Expression<Func<Topic, IDataContext, IQueryable<Message>>> Query => (t, ctx) => ctx.GetTable<Message>().Where(m => m.TopicId == t.Id);
		}

		public class Message
		{
			public int Id { get; set; }

			public int TopicId { get; set; }

			[Association(ThisKey = "TopicId", OtherKey = "Id")]
			public virtual Topic Topic { get; set; }

			public string Text { get; set; }
		}

		[Test]
		public void TestAttributesByKey([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Topic>())
			using (db.CreateLocalTable<Message>())
			{
				db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<Topic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA1.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test]
		public void TestAttributesByExpression([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Topic>())
			using (db.CreateLocalTable<Message>())
			{
				db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<Topic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA2.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test]
		public void TestAttributesByQuery([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Topic>())
			using (db.CreateLocalTable<Message>())
			{
				db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<Topic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA3.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test]
		public void TestFluentAssociationByExpression([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<Topic>()
						.Association(e => e.MessagesF, (t, m) => t.Id == m.TopicId)
						.Property(e => e.Id)
						.Property(e => e.Title)
						.Property(e => e.Text)
					.Entity<Message>()
						.Property(e => e.Id)
						.Property(e => e.TopicId)
						.Property(e => e.Text);

				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

					db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF.Select(t => t.Id).ToList()
						}).FirstOrDefault();
				}
			}
		}

		[Test]
		public void TestFluentAssociationByKeys([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<Topic>()
						// TODO: add missing override and uncomment
						//.Association(e => e.MessagesF, t => t.Id, m => m.TopicId)
						.Property(e => e.Id)
						.Property(e => e.Title)
						.Property(e => e.Text)
					.Entity<Message>()
						.Property(e => e.Id)
						.Property(e => e.TopicId)
						.Property(e => e.Text);

				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

					db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF.Select(t => t.Id).ToList()
						}).FirstOrDefault();
				}
			}
		}

		[Test]
		public void TestFluentAssociationByQuery([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<Topic>()
						.Association(e => e.MessagesF, (t, ctx) => ctx.GetTable<Message>().Where(m => m.TopicId == t.Id))
						.Property(e => e.Id)
						.Property(e => e.Title)
						.Property(e => e.Text)
					.Entity<Message>()
						.Property(e => e.Id)
						.Property(e => e.TopicId)
						.Property(e => e.Text);

				using (db.CreateLocalTable<Topic>())
				using (db.CreateLocalTable<Message>())
				{
					db.Insert(new Topic() { Id = 6, Title = "title", Text = "text" });

					db.GetTable<Topic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF.Select(t => t.Id).ToList()
						}).FirstOrDefault();
				}
			}
		}
	}
}
