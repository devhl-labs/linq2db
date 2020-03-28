﻿using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class UpdateWhenMatchedThenDelete : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& LinqExtensions.UpdateWhenMatchedAndThenDeleteMethodInfo == methodCall.Method.GetGenericMethodDefinition();
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenMatchedAndThenDelete(merge, searchCondition, setter, deleteCondition)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.UpdateWithDelete);
				statement.Operations.Add(operation);

				var predicate       = methodCall.Arguments[1];
				var setter          = methodCall.Arguments[2];
				var deletePredicate = methodCall.Arguments[3];

				if (!(setter is ConstantExpression constSetter) || constSetter.Value != null)
				{
					var setterExpression = (LambdaExpression)setter.Unwrap();
					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						setterExpression,
						mergeContext.TargetContext,
						operation.Items,
						mergeContext.TargetContext, mergeContext.SourceContext);
				}
				else
				{
					// build setters like QueryRunner.Update
					var targetType = methodCall.Method.GetGenericArguments()[0];
					var sqlTable   = (SqlTable)statement.Target.Source;
					var param      = Expression.Parameter(targetType, "s");
					var keys       = sqlTable.GetKeys(false).Cast<SqlField>().ToList();

					foreach (var field in sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys))
					{
						var expression = LinqToDB.Expressions.Extensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, param);
						var expr       = mergeContext.SourceContext.ConvertToSql(builder.ConvertExpression(expression), 1, ConvertFlags.Field)[0].Sql;

						operation.Items.Add(new SqlSetExpression(field, expr));
					}
				}

				if (!(predicate is ConstantExpression constPredicate) || constPredicate.Value != null)
				{
					var predicateCondition     = (LambdaExpression)predicate.Unwrap();
					var predicateConditionExpr = builder.ConvertExpression(predicateCondition.Body.Unwrap());

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { mergeContext.TargetContext, mergeContext.SourceContext }, predicateCondition),
						predicateConditionExpr,
						operation.Where.Conditions,
						false);
				}

				if (!(deletePredicate is ConstantExpression constDeletePredicate) || constDeletePredicate.Value != null)
				{
					var deleteCondition     = (LambdaExpression)deletePredicate.Unwrap();
					var deleteConditionExpr = builder.ConvertExpression(deleteCondition.Body.Unwrap());

					operation.WhereDelete = new SqlSearchCondition();

					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { mergeContext.TargetContext, mergeContext.SourceContext }, deleteCondition),
						deleteConditionExpr,
						operation.WhereDelete.Conditions,
						false);
				}

				return mergeContext;
			}

			protected override SequenceConvertInfo? Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			{
				return null;
			}
		}
	}
}
