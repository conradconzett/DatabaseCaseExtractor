using DatabaseCaseExtractor.Attributes;
using DatabaseCaseExtractor.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DatabaseCaseExtractor
{
	public class Helpers
	{

		public static T UpdateModel<T>(T oldEntity, T newEntity, DbContext context)
				where T : class, new()
		{
			if (oldEntity == null)
			{
				DbSet<T> importSet = (DbSet<T>)context.GetType().GetMethod("Set").MakeGenericMethod(typeof(T)).Invoke(context, null);
				importSet.Add(newEntity);
				return newEntity;
			}
			if (newEntity == null)
			{
				DbSet<T> importSet = (DbSet<T>)context.GetType().GetMethod("Set").MakeGenericMethod(typeof(T)).Invoke(context, null);
				importSet.Remove(oldEntity);
				return oldEntity;
			}

			foreach (PropertyInfo property in typeof(T).GetProperties())
			{
				object dbValue = typeof(T).GetProperty(property.Name).GetValue(oldEntity);
				object newValue = typeof(T).GetProperty(property.Name).GetValue(newEntity);
				if (!property.PropertyType.FullName.Contains("System") || property.PropertyType.GetGenericArguments().Length > 0)
				{
					Type subType;
					bool wasGeneric = false;
					if (property.PropertyType.GetGenericArguments().Length > 0)
					{
						subType = property.PropertyType.GetGenericArguments()[0];
						wasGeneric = true;
					}
					else
					{
						subType = property.PropertyType;
					}
					// Check if type is in the context
					PropertyInfo tempSet = context.GetType().GetProperties().Where(p => p.PropertyType.IsGenericType
							&& p.PropertyType.Name.Contains("DbSet") && p.PropertyType.GetGenericArguments()[0].Name == subType.Name)
					.FirstOrDefault();
					if (tempSet != null)
					{
						// DbSet exists so recall updateModel with this type
						MethodInfo method = typeof(Helpers).GetMethod("UpdateModel");
						MethodInfo generic = method.MakeGenericMethod(subType);
						if (wasGeneric)
						{
							// We have a array of values in the old and in the new value
							// so we have to loop through the new values find them in the 
							// old one and update them. 
							Type listType = typeof(List<>).MakeGenericType(subType);
							PropertyInfo keyProperty = subType.GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute))).FirstOrDefault();

							IList oldValues = (IList)Convert.ChangeType(dbValue, listType);
							if (newValue == null)
							{
								foreach (object oldVal in oldValues)
								{
									generic.Invoke(null, new object[] { oldVal, null, context });
								}
							}
							else
							{
								foreach (object newVal in (IList)Convert.ChangeType(newValue, listType))
								{
									// Find data in the old array -> insert them
									object oldValue = GetValueFromOtherArray(newVal, oldValues, keyProperty);
									generic.Invoke(null, new object[] { oldValue, newVal, context });
								}
							}
							IList newValues = (IList)Convert.ChangeType(newValue, listType);
							foreach (object oldVal in (IList)Convert.ChangeType(dbValue, listType))
							{
								// Find data who aren't in the new value anymore -> delete them
								object tempNewValue = GetValueFromOtherArray(oldVal, newValues, keyProperty);
								if (tempNewValue == null)
								{
									generic.Invoke(null, new object[] { oldVal, tempNewValue, context });
								}
							}
						}
						else
						{
							if (newValue != null)
							{
								generic.Invoke(null, new object[] { dbValue, newValue, context });
							}
						}
					}
				}
				else
				{
					if (dbValue.ToString() != newValue.ToString())
					{
						typeof(T).GetProperty(property.Name).SetValue(oldEntity, newValue);
					}
				}
			}
			return oldEntity;
		}

		private static object GetValueFromOtherArray(object firstValue, IList secondValues, PropertyInfo keyProperty)
		{
			object resultValue = null;
			object newKeyValue = keyProperty.GetValue(firstValue);
			if (secondValues == null)
			{
				return null;
			}
			for (int iOldValues = 0; iOldValues < secondValues.Count; iOldValues++)
			{
				object oldKeyValue = keyProperty.GetValue(secondValues[iOldValues]);
				if (oldKeyValue.Equals(newKeyValue))
				{
					resultValue = secondValues[iOldValues];
				}
			}
			return resultValue;
		}


		public static Dictionary<string, SimpleExportResult> GetExportResult<T>(object data, Dictionary<string, SimpleExportResult> exportResults = null)
				where T : class, new()
		{
			if (data == null)
			{
				return null;
			}
			T[] entities;
			if (!data.GetType().IsGenericType)
			{
				entities = new T[] { (T)data };
			}
			else
			{
				entities = ((HashSet<T>)data).ToArray();
			}

			if (exportResults == null)
			{
				exportResults = new Dictionary<string, SimpleExportResult>();
			}
			if (!exportResults.ContainsKey(typeof(T).Name))
			{
				exportResults.Add(typeof(T).Name, new SimpleExportResult() { EntityName = typeof(T).Name, EntityData = new List<T>() });
			}

			foreach (PropertyInfo info in typeof(T).GetProperties()
					.Where(p => Attribute.IsDefined(p, typeof(DatabaseCaseExtractorIncludeAttribute))))
			{
				Type type = info.PropertyType;
				if (info.PropertyType.GetGenericArguments().Length > 0)
				{
					type = info.PropertyType.GetGenericArguments()[0];
				}
				int i = 0;
				foreach (T entity in entities)
				{
					object value = info.GetValue(entity);
					MethodInfo method = typeof(Helpers).GetMethod("GetExportResult").MakeGenericMethod(new[] { type });
					Dictionary<string, SimpleExportResult> tempExportResults = (Dictionary<string, SimpleExportResult>)method.Invoke(null, new object[] { value, exportResults });
					if (tempExportResults != null)
					{
						exportResults = tempExportResults;
					}

					// exportResults = GetExportResult(info.GetValue(entity), exportResults);
					info.SetValue(entities[i], null);
					i++;
				}

			}

			List<T> tempEntities = new List<T>();
			if (exportResults.ContainsKey(typeof(T).Name))
			{
				SimpleExportResult tempExport;
				if (exportResults.TryGetValue(typeof(T).Name, out tempExport))
				{
					tempEntities = (List<T>)tempExport.EntityData;
				}
			}

			tempEntities.AddRange(entities);
			exportResults[typeof(T).Name] = new SimpleExportResult()
			{
				EntityName = typeof(T).Name,
				EntityData = tempEntities
			};


			return exportResults;
		}
	}
}
