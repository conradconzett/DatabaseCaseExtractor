using DatabaseCaseExtractor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using DatabaseCaseExtractor.Interfaces;
using System.Reflection;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using DatabaseCaseExtractor.Attributes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace DatabaseCaseExtractor
{
    public class ExportImportService<T> : IExportImportService
        where T : class
    {
        private DbContext _context;
        public ExportImportService(DbContext dbContext)
        {
            _context = dbContext;
        }

        /// <summary>
        /// Collects data from database and returns it
        /// </summary>
        /// <param name="exportLayout"></param>
        /// <returns></returns>
        public ExportResult GetExportResult(ExportLayout exportLayout, bool loadAdditionalData = true)
        {
            var properties = _context.GetType().GetProperties();

            PropertyInfo setType = properties.Where(p => p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericArguments()[0] == typeof(T)).FirstOrDefault();

            if (setType != null)
            {
                Type[] typeArgs = { typeof(T) };
                Type genericType = typeof(DbSet<>);

                IQueryable<T> queryable = GetSet(_context).AsNoTracking<T>();
                if (exportLayout.EntityPrimaryValue != null)
                {
                    if (exportLayout.EntityPrimaryKeyType == null || exportLayout.EntityPrimaryKeyType == "")
                    {
                        // Get the key property
                        foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
                        {
                            if (Attribute.IsDefined(propertyInfo, typeof(KeyAttribute)))
                            {
                                exportLayout.EntityPrimaryKey = propertyInfo.Name;
                                exportLayout.EntityPrimaryKeyType = propertyInfo.PropertyType.Name.ToUpper();
                                break;
                            }
                        }
                    }
                    if (exportLayout.EntityPrimaryKey != null &&
                        (exportLayout.EntityPrimaryKeyType == null || exportLayout.EntityPrimaryKeyType == ""))
                    {
                        PropertyInfo propertyInfo = typeof(T).GetProperty(exportLayout.EntityPrimaryKey);
                        exportLayout.EntityPrimaryKeyType = propertyInfo.PropertyType.Name.ToUpper();
                    }
                    string equalString = "";
                    if (exportLayout.EntityPrimaryKeyType != null && exportLayout.EntityPrimaryValue != "")
                    {
                        switch (exportLayout.EntityPrimaryKeyType.ToUpper())
                        {
                            case "GUID":
                                equalString = string.Format(".Equals(Guid.Parse(\"{0}\"))", exportLayout.EntityPrimaryValue);
                                break;
                            case "STRING":
                                equalString = string.Format("== \"{0}\"", exportLayout.EntityPrimaryValue);
                                break;
                            case "INT":
                                equalString = string.Format("== {0}", exportLayout.EntityPrimaryValue);
                                break;
                        }
                        Expression<Func<T, bool>> expression = DynamicExpressionParser.ParseLambda<T, bool>(null, false,
                            exportLayout.EntityPrimaryKey + equalString);
                        queryable = queryable.Where(expression);
                    }
                }

                // Get Includes from Attributes
                if (exportLayout.Includes == null && exportLayout.UseModelAttributes)
                {
                    exportLayout.Includes = GetIncludesFromAttribute(typeof(T));
                }

                if (exportLayout.Includes != null)
                {
                    foreach (ExportInclude include in exportLayout.Includes)
                    {
                        queryable = SetIncludes(queryable, include);
                    }
                }

                if (exportLayout.AdditionalData == null && loadAdditionalData && exportLayout.UseModelAttributes)
                {
                    List<ExportLayout> additionalDataTables = new List<ExportLayout>();
                    foreach (PropertyInfo additionalData in properties.Where(p => p.PropertyType.IsGenericType &&
                         Attribute.IsDefined(p.PropertyType.GetGenericArguments()[0], typeof(AdditionalDataAttribute))).ToList())
                    {
                        additionalDataTables.Add(new ExportLayout()
                        {
                            EntityName = additionalData.PropertyType.GetGenericArguments()[0].Name
                        });
                    }
                    if (additionalDataTables.Count > 0)
                    {
                        exportLayout.AdditionalData = additionalDataTables.ToArray();
                    }
                }

                List<ExportResult> additionalDatas = new List<ExportResult>();
                if (exportLayout.AdditionalData != null && loadAdditionalData)
                {
                    Type additionalType = typeof(ExportImportService<>);
                    foreach (ExportLayout subLayout in exportLayout.AdditionalData)
                    {
                        PropertyInfo setAdditionalType = properties.Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericArguments()[0].Name == subLayout.EntityName).FirstOrDefault();

                        var addionalInstance = additionalType.MakeGenericType(setAdditionalType.PropertyType.GetGenericArguments()[0]);
                        object subExportLayout = Activator.CreateInstance(addionalInstance, new object[] { _context });
                        additionalDatas.Add(((IExportImportService)subExportLayout).GetExportResult(subLayout, false));
                    }
                }

                if (exportLayout.EntityPrimaryValue != null)
                {
                    return new ExportResult() { EntityData = queryable.FirstOrDefault(), AdditionalData = additionalDatas.ToArray(), EntityName = typeof(T).Name };
                }
                else
                {
                    return new ExportResult() { EntityData = queryable.ToArray(), AdditionalData = additionalDatas.ToArray(), EntityName = typeof(T).Name };
                }
            }
            return null;
        }

        /// <summary>
        /// Imports data into an context. If clear = true the hole contet will be cleared first
        /// </summary>
        /// <param name="importData"></param>
        /// <param name="clear"></param>
        /// <returns></returns>
        public bool SetImportResult(ExportResult importData, bool clear = true)
        {

            var properties = _context.GetType().GetProperties();
            if (clear)
            {
                List<PropertyInfo> sets = properties.Where(p => p.PropertyType.IsGenericType
                    && p.PropertyType.FullName.Contains("Microsoft.EntityFrameworkCore.DbSet"))
                    .ToList();

                foreach (PropertyInfo set in sets)
                {
                    var queryable = (IQueryable)_context.GetType().GetMethod("Set").MakeGenericMethod(set.PropertyType.GetGenericArguments()[0]).Invoke(_context, null);
                    foreach (object tempRow in queryable)
                    {
                        _context.Remove(tempRow);
                    }
                }
                _context.SaveChanges();
            }

            // Handle additional-data
            if (importData.AdditionalData != null)
            {
                Type additionalType = typeof(ExportImportService<>);
                foreach (ExportResult additionalData in importData.AdditionalData)
                {
                    PropertyInfo setAdditionalType = properties.Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericArguments()[0].Name == additionalData.EntityName).FirstOrDefault();

                    var addionalInstance = additionalType.MakeGenericType(setAdditionalType.PropertyType.GetGenericArguments()[0]);
                    object additionalImportService = Activator.CreateInstance(addionalInstance, new object[] { _context });
                    ((IExportImportService)additionalImportService).SetImportResult(additionalData, false);
                }
            }

            // Import Entity-Data
            PropertyInfo tempSet = properties.Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.FullName.Contains(importData.EntityName))
                .FirstOrDefault();

            DbSet<T> importSet = (DbSet<T>)_context.GetType().GetMethod("Set").MakeGenericMethod(tempSet.PropertyType.GetGenericArguments()[0]).Invoke(_context, null);
            importSet.Add((T)importData.EntityData);

            _context.SaveChanges();

            return true;
        }

        #region Private-Methods
        /// <summary>
        /// Get a IQueryable from T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        private IQueryable<T> GetSet(DbContext context)
        {
            // Get the generic type definition 
            MethodInfo method = typeof(DbContext).GetMethod(nameof(DbContext.Set), BindingFlags.Public | BindingFlags.Instance);

            // Build a method with the specific type argument you're interested in 
            method = method.MakeGenericMethod(typeof(T));

            return method.Invoke(context, null) as IQueryable<T>;
        }

        /// <summary>
        /// Includes subdata
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="exportInclude"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private IQueryable<T> SetIncludes(IQueryable<T> queryable, ExportInclude exportInclude, string prefix = "")
        {
            queryable = queryable.Include(prefix + exportInclude.Include);
            if (exportInclude.SubIncludes != null)
            {
                prefix = prefix + exportInclude.Include + ".";
                foreach (ExportInclude subInclude in exportInclude.SubIncludes)
                {
                    queryable = SetIncludes(queryable, subInclude, prefix);
                }
            }
            return queryable;
        }

        private ExportInclude[] GetIncludesFromAttribute(Type type, List<Type> addedTypes = null)
        {
            if (addedTypes == null)
            {
                addedTypes = new List<Type>();
            }
            List<ExportInclude> includes = new List<ExportInclude>();
            foreach(PropertyInfo propertyInfo in type.GetProperties())
            {
                if (!addedTypes.Contains(propertyInfo.PropertyType) && 
                    Attribute.IsDefined(propertyInfo, typeof(DatabaseCaseExtractorIncludeAttribute)))
                {
                    addedTypes.Add(propertyInfo.PropertyType);

                    Type[] genericTypes = propertyInfo.PropertyType.GetGenericArguments();
                    if (genericTypes.Length == 0)
                    {
                        includes.Add(new ExportInclude()
                        {
                            Include = propertyInfo.Name,
                            SubIncludes = GetIncludesFromAttribute(propertyInfo.PropertyType, addedTypes)
                        });
                    }
                    else
                    {
                        includes.Add(new ExportInclude()
                        {
                            Include = propertyInfo.Name,
                            SubIncludes = GetIncludesFromAttribute(genericTypes[0], addedTypes)
                        });
                    }
                }
            }
            return includes.ToArray();
        }
        #endregion
    }
}
