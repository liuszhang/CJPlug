using CJ.Plug.ApiClient.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.ApiClient.Services
{
    public class NameGenerator(MainApiClient MainApiClient): INameGenerator
    {
        public string GetNextName(string name, List<string>? existingNames)
        {
            if (existingNames == null || existingNames.Count == 0)
            {
                return name;
            }
            int index = 1;
            string newName = name;
            while (existingNames.Contains(newName))
            {
                newName = $"{name}{index}";
                index++;
            }
            return newName;
        }
        public bool IsValidName(string name)
        {
            // Implement your validation logic here
            return !string.IsNullOrEmpty(name);
        }
    }

}
