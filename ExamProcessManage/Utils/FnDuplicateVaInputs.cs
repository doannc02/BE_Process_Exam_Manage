namespace ExamProcessManage.Utils
{
    public class FnDuplicateVaInputs
    {
        public static List<string> CheckDuplicate<T>(List<T> items, Func<T, int> getId, Func<T, string> getCode, Func<T, string> getName, string prefix)
        {
            var idDict = new Dictionary<int, int>();   
            var codeDict = new Dictionary<string, int>(); 
            var nameDict = new Dictionary<string, int>(); 
            var duplicates = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var id = getId(item);
                var code = getCode(item);
                var name = getName(item);

            
                if (idDict.ContainsKey(id))
                {
                    duplicates.Add($"{prefix}.{i}.Id");  
                }
                else
                {
                    idDict[id] = i; 
                }

               
                if (codeDict.ContainsKey(code))
                {
                    duplicates.Add($"{prefix}.{i}.Code");  
                }
                else
                {
                    codeDict[code] = i;  
                }

                if (nameDict.ContainsKey(name))
                {
                    duplicates.Add($"{prefix}.{i}.Name");  
                }
                else
                {
                    nameDict[name] = i;  
                }
            }

            return duplicates;
        }
    }
}
