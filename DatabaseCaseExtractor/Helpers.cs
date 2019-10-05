using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DatabaseCaseExtractor
{
    public class Helpers
    {

        public static T UpdateModel<T>(T oldEntity, T newEntity, DbContext context)
            where T: class, new() 
        {
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
                            int rowIndex = 0;

                            Type listType = typeof(List<>).MakeGenericType(subType);
                            IList oldValues = (IList)Convert.ChangeType(dbValue, listType);
                            foreach (object newVal in (IList)Convert.ChangeType(newValue, listType))
                            {
                                // Find data in the old array
                                generic.Invoke(null, new object[] { oldValues[rowIndex], newVal, context });
                                rowIndex++;
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

    }
}
