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

		static bool chechIfEntityIsInExportResult<T>(T entity, Dictionary<string, SimpleExportResult> exportResults)
		{
			if (exportResults.ContainsKey(typeof(T).Name))
			{
				List<T> searchList = (List<T>)exportResults[typeof(T).Name].EntityData;
				PropertyInfo keyProperty = typeof(T).GetProperties()
					.Where(p => Attribute.IsDefined(p, typeof(KeyAttribute))).First();
				return searchList.Where(p => keyProperty.GetValue(p).ToString() == keyProperty.GetValue(entity).ToString()).Count() > 0;
			}
			return false;
		}

		public static Dictionary<string, SimpleExportResult> GetExportResult<T>(object data, Dictionary<string, SimpleExportResult> exportResults)
						where T : class, new()
		{
			if (data == null)
			{
				return exportResults;
			}
			// Get data to export
			List<T> entities; // Contains data wich needs to be exported
			if (data.GetType().IsGenericType) // So it is a collection
			{
				entities = ((HashSet<T>)data).ToList();
			}
			else
			{
				entities = new List<T> { (T)data };
			}

			// Get active table-data or create a new entry
			if (!exportResults.ContainsKey(typeof(T).Name))
			{
				exportResults.Add(
						typeof(T).Name,
						new SimpleExportResult() { EntityName = typeof(T).Name, EntityData = new List<T>() }
				);
			}
			else
			{
				// Remove entities wich are allready added
				List<T> savedEntities = (List<T>)exportResults[typeof(T).Name].EntityData;
				entities = entities.Except(savedEntities).ToList();
				if (entities.Count == 0)
				{
					return exportResults;
				}
			}

			IEnumerable<PropertyInfo> infos = typeof(T).GetProperties()
							.Where(p => Attribute.IsDefined(p, typeof(DatabaseCaseExtractorIncludeAttribute)));
			// Loop though data to add them to exportresults
			foreach (T entity in entities)
			{
				// Check if entity is already in exportResults
				if (chechIfEntityIsInExportResult<T>(entity, exportResults))
				{
					continue;
				}

				foreach (PropertyInfo collectionProperty in typeof(T).GetProperties()
					.Where(p => p.PropertyType.IsGenericType && !Attribute.IsDefined(p, typeof(DatabaseCaseExtractorIncludeAttribute))))
				{
					collectionProperty.SetValue(entity, null);
				}
				((List<T>)exportResults[typeof(T).Name].EntityData).Add(entity);
				// Loop through properties to make sure everything can be exported
				foreach (PropertyInfo info in infos)
				{
					// Get the correct type for related data
					Type type = info.PropertyType;
					if (info.PropertyType.GetGenericArguments().Length > 0)
					{
						type = info.PropertyType.GetGenericArguments()[0];
					}

					// Get related data
					object value = info.GetValue(entity); // object is a collection or an other entity
					MethodInfo method = typeof(Helpers).GetMethod("GetExportResult").MakeGenericMethod(new[] { type });
					exportResults = (Dictionary<string, SimpleExportResult>)method.Invoke(null, new object[] { value, exportResults });

					// Set related data to null because data is saved in exportResults
					info.SetValue(((List<T>)exportResults[typeof(T).Name].EntityData).Last(), null);
				}
			}

			return exportResults;
		}
	}
}
