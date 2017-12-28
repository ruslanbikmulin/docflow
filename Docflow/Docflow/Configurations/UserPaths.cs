using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Docflow.Configurations
{
    public class UserPaths
    {
        public static UserPathsConfigSection _Config =
            ConfigurationManager.GetSection("userConfigs") as UserPathsConfigSection;

        public static UserPathsElementCollection GetUserPaths()
        {
            return _Config.UserPaths;
        }
    }

    public class UserPathsConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("userPaths")]
        public UserPathsElementCollection UserPaths
        {
            get { return (UserPathsElementCollection) this["userPaths"]; }
        }
    }

    [ConfigurationCollection(typeof(UserPathsElement))]
    public class UserPathsElementCollection : ConfigurationElementCollection
    {
        public UserPathsElement this[int index]
        {
            get { return (UserPathsElement) BaseGet(index); }
            set
            {
                if (BaseGet(index) != null) 
                {
                    BaseRemoveAt(index);
                }

                BaseAdd(index, value);

            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new UserPathsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UserPathsElement) element).Path;
        }
    }

    public class UserPathsElement : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string) this["path"]; }
        }
    }

}