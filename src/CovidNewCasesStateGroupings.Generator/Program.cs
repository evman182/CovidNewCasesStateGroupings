using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;

namespace CovidNewCasesStateGroupings.Generator
{
    class Program
    {
        private const string SourceUrl =
            @"https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_US.csv";

        static async Task Main()
        {
            List<dynamic> records = await GetRecords();
            var states = new Dictionary<string, Dictionary<string, int>>();
            var count = ((ExpandoObject)records.First()).ToList().Count;

            var justStates = records.Where(r => r.iso2 == "US").ToList();
            foreach (dynamic row in justStates)
            {
                var state = row.Province_State;

                if (!states.TryGetValue(state, out Dictionary<string, int> stateDict))
                {
                    stateDict = new Dictionary<string, int>();
                    states[state] = stateDict;
                }

                var expandoState = ((ExpandoObject)row).ToList();
                for (int x = 13; x < count; x++)
                {
                    var item = expandoState[x];
                    var newCases = int.Parse(item.Value.ToString()) - int.Parse(expandoState[x - 1].Value.ToString());
                    if (!stateDict.ContainsKey(item.Key))
                    {
                        stateDict[item.Key] = 0;
                    }

                    stateDict[item.Key] += newCases;
                }
            }

            var stateData = JsonSerializer.Serialize(states);
            Console.WriteLine(stateData);
        }

        private static async Task<List<dynamic>> GetRecords()
        {
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(SourceUrl);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<dynamic>().ToList();
        }
    }
}
