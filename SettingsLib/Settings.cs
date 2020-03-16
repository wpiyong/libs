using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace SettingsLib
{
    public abstract class Settings
    {
        protected XDocument _settings;
        protected string settingsFileName = "";

        public Settings(string fileName)
        {
            settingsFileName = fileName;
        }

        public virtual bool Load()
        {
            bool result = false;
            try
            {
                _settings = XDocument.Load(settingsFileName);
                foreach (var prop in this.GetType().GetProperties())
                {
                    object propValue = Get(prop.Name);
                    if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        if (propValue == null || String.IsNullOrEmpty(propValue.ToString()))
                        {
                            prop.SetValue(this, null);
                        }
                        else
                            prop.SetValue(this, Convert.ChangeType(propValue, prop.PropertyType.GetGenericArguments()[0],
                                System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        prop.SetValue(this, Convert.ChangeType(propValue, prop.PropertyType,
                            System.Globalization.CultureInfo.InvariantCulture));
                    }

                }

                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }




        protected object Get(string name)
        {
            object res = null;

            var field = _settings.Descendants("setting")
                                    .Where(x => (string)x.Attribute("name") == name)
                                    .FirstOrDefault();

            if (field != null)
            {
                res = field.Element("value").Value;
            }
            //else
            //    throw new Exception("Property not found in Settings");

            return res;
        }

        protected void Set(string name, object value)
        {
            var field = _settings.Descendants("setting")
                                    .Where(x => (string)x.Attribute("name") == name)
                                    .FirstOrDefault();

            if (field != null)
            {
                field.Element("value").Value = value.ToString();
            }
            else
                throw new Exception("Property not found in Settings");
        }

        public virtual void Save()
        {
            try
            {
                foreach (var prop in this.GetType().GetProperties())
                {
                    try
                    {
                        Set(prop.Name, prop.GetValue(this));
                    }
                    catch
                    {
                        if (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                        {
                            throw;
                        }
                    }
                }
                _settings.Save(settingsFileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Settings not saved");
            }
        }
    }
}
