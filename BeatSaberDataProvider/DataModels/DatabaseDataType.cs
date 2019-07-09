using System;
using System.Reflection;

namespace BeatSaberDataProvider.DataModels
{
    public abstract class DatabaseDataType
    {
        public abstract object[] PrimaryKey { get; }
        public virtual object this[string propertyName]
        {
            get
            {
                Type myType = this.GetType();
                object retVal;

                PropertyInfo myPropInfo = myType.GetProperty(propertyName);

                if (myPropInfo != null)
                {
                    retVal = myPropInfo.GetValue(this);
                }
                else
                {
                    FieldInfo field = myType.GetField(propertyName);
                    retVal = field.GetValue(this);
                }

                //Type whatType = retVal.GetType();
                return retVal;
            }
            set
            {
                Type myType = this.GetType();
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }
    }

    public class Updatable : Attribute
    {

    }
}
