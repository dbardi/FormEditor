﻿using System.Collections.Generic;
using System.Linq;
using System.Web;
using FormEditor.Fields;
using FormEditor.Validation;
using Newtonsoft.Json;
using Umbraco.Core.Models;
using System.Web.Helpers;

namespace FormEditor.Rendering
{
	public static class RenderingExtensions
	{
		public static List<ValidationData> ForFrontEnd(this IEnumerable<Validation.Validation> validations)
		{
			if(validations == null)
			{
				return new List<ValidationData>();
			}
			return validations.Select(v => new ValidationData
			{
				Rules = v.Rules.ForFrontEnd(),
				Invalid = v.Invalid,
				ErrorMessage = v.ErrorMessage
			}).ToList();
		}

		private static List<RuleData> ForFrontEnd(this IEnumerable<Rule> rules)
		{
			if(rules == null)
			{
				return new List<RuleData>();
			}
			return rules.Select(r => new RuleData
			{
				Field = ToFieldData(r.Field),
				Condition = r.Condition != null 
					? r.Condition.ForFrontEnd()
					: new ConditionData()
			}).ToList();
		}

		private static List<ActionData> ForFrontEnd(this IEnumerable<Action> actions)
		{
			if(actions == null)
			{
				return new List<ActionData>();
			}
			return actions.Select(a => new ActionData
			{
				Rules = a.Rules.ForFrontEnd(),
				Field = ToFieldData(a.Field),
				Task = a.Task
			}).ToList();
		} 

		public static IHtmlString Render(this IEnumerable<Validation.Validation> validations)
		{
			return new HtmlString(
				JsonConvert.SerializeObject(validations.ForFrontEnd(), SerializerSettings)
			);
		}

		public static IHtmlString Render(this IEnumerable<Validation.Action> actions)
		{

			return new HtmlString(
				JsonConvert.SerializeObject(actions.ForFrontEnd(), SerializerSettings)
			);
		}

		public static List<FieldData> ForFrontEnd(this IEnumerable<FieldWithValue> fields)
		{
			return fields?.Select(ToFieldData).ToList() ?? new List<FieldData>();
		}

		public static List<FieldData> ForFrontEnd(this IEnumerable<Field> fields)
		{
			return fields?.Select(ToFieldData).ToList() ?? new List<FieldData>();
		}

		public static IHtmlString Render(this IEnumerable<FieldWithValue> fields)
		{
			return new HtmlString(
				JsonConvert.SerializeObject(fields.ForFrontEnd(), SerializerSettings)
			);
		}

		public static bool HasDefaultValue(this FieldWithValue field)
		{
			if(field is IDefaultSelectableField)
			{
				return true;
			}
			if(field.HasSubmittedValue)
			{
				return true;
			}
			return field is FieldWithFieldValues fieldWithFieldValues && fieldWithFieldValues.FieldValues.Any(f => f.Selected);
		}

		public static IHtmlString DefaultValue(this FieldWithValue field)
		{
			if(field is IDefaultSelectableField defaultSelectableField)
			{
				return new HtmlString(defaultSelectableField.Selected ? "true" : "undefined");
			}
			var fieldWithFieldValues = field as FieldWithFieldValues;
			if(field.HasSubmittedValue)
			{
				if(field is DateField)
				{
					return new HtmlString($"new Date(\"{HttpUtility.JavaScriptStringEncode(field.SubmittedValue)}\")");
				}
				if(fieldWithFieldValues != null)
				{
					if (fieldWithFieldValues.IsMultiSelectEnabled)
					{
						return new HtmlString($"[{string.Join(",", fieldWithFieldValues.SubmittedValues.Select(v => "\"" + HttpUtility.JavaScriptStringEncode(v) + "\""))}]");
					}
					return new HtmlString(
						fieldWithFieldValues.SubmittedValues.Any()
							? $"\"{HttpUtility.JavaScriptStringEncode(fieldWithFieldValues.SubmittedValues.First())}\""
							: "undefined"
					);
				}
				return new HtmlString($"\"{HttpUtility.JavaScriptStringEncode(field.SubmittedValue)}\"");
			}
			if (fieldWithFieldValues == null)
			{
				return new HtmlString("undefined");
			}
			var defaultValues = fieldWithFieldValues.FieldValues.Where(f => f.Selected).ToArray();
			if (fieldWithFieldValues.IsMultiSelectEnabled)
			{
				return new HtmlString($"[{string.Join(",", defaultValues.Select(v => "\"" + HttpUtility.JavaScriptStringEncode(v.Value) + "\""))}]");
			}
			return new HtmlString(
				defaultValues.Any()
					? $"\"{HttpUtility.JavaScriptStringEncode(defaultValues.First().Value)}\""
					: "undefined"
			);
		}

		public static void SetSubmittedValue(this FieldWithValue field, string value, IPublishedContent content)
		{
			field.CollectSubmittedValue(new Dictionary<string, string> { { field.FormSafeName, value } }, content);
		}

		private static FieldData ToFieldData(FieldWithValue f)
		{
			return f != null
				? new FieldData
				{
					Name = f.Name,
					FormSafeName = f.FormSafeName,
					SubmittedValue = f.SubmittedValue,
					Invalid = f.Invalid
				}
				: new FieldData();
		}

		private static FieldData ToFieldData(Field f)
		{
			if(f is FieldWithValue fieldWithValue)
			{
				return ToFieldData(fieldWithValue);
			}
			return f is IFieldWithValidation fieldWithValidation
				? new FieldData
				{
					Name = fieldWithValidation.Name,
					FormSafeName = fieldWithValidation.FormSafeName,
					SubmittedValue = null,
					Invalid = fieldWithValidation.Invalid
				}
				: new FieldData();
		}

		public static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.None,
			ContractResolver = SerializationHelper.ContractResolver
		};

		public static string LabelOrName(this FieldWithValue field)
		{
			return field is FieldWithLabel fieldWithLabel ? fieldWithLabel.Label : field.Name;			
		}

		public static string AntiForgeryToken(this FormModel model)
		{
			AntiForgery.GetTokens(null, out var cookieToken, out var formToken);
			return $"{cookieToken}:{formToken}";
		}
	}
}
