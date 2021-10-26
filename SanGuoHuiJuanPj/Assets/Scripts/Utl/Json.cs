//using System;
//using System.Collections.Generic;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Serialization;
//namespace Assets.Scripts.Utl
//{
//    public static class Json
//    {
//        //public static JsonSerializerSettings Settings => new JsonSerializerSettings
//        //{
//        //    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
//        //    DefaultValueHandling = DefaultValueHandling.Ignore
//        //};
//        public static string JListAction<T>(string jList, Action<List<T>> action)
//        {
//            var list = Deserialize<List<T>>(jList) ?? new List<T>();
//            action.Invoke(list);
//            return Serialize(list);
//        }
//        public static string JObjAction<T>(string jObj, Action<T> action) where T : class, new()
//        {
//            var obj = Deserialize<T>(jObj) ?? new T();
//            action.Invoke(obj);
//            return Serialize(obj);
//        }
//        public static TResult JListAction<T, TResult>(string jList, Func<List<T>, TResult> function)
//        {
//            var list = Deserialize<List<T>>(jList) ?? new List<T>();
//            return function.Invoke(list);
//        }

//        public static object[] DeserializeObjs(string value) => Deserialize<object[]>(value);
//        public static string Serialize(object obj) => JsonConvert.SerializeObject(obj);

//        public static T Deserialize<T>(string value) where T : class
//        {
//            try
//            {
//                return value == null ? null : JsonConvert.DeserializeObject<T>(value);
//            }
//            catch (Exception e)
//            {
//                return null;
//            }
//        }
//        public static T Deserialize<T>(string value,JsonConverter[] converters) where T : class
//        {
//            try
//            {
//                return value == null ? null : JsonConvert.DeserializeObject<T>(value, converters);
//            }
//            catch (Exception e)
//            {
//                return null;
//            }
//        }
//        public static T Deserialize<T>(string value,IContractResolver resolver) where T : class
//        {
//            try
//            {
//                return value == null ? null : JsonConvert.DeserializeObject<T>(value, new JsonSerializerSettings
//                {
//                    ContractResolver = resolver
//                });
//            }
//            catch (Exception e)
//            {
//                return null;
//            }
//        }

//        public static List<T> DeserializeList<T>(string jList,params JsonConverter[] converters) => string.IsNullOrWhiteSpace(jList) ? new List<T>() : Deserialize<List<T>>(jList,converters);
//        public static List<T> DeserializeList<T>(string jList,IContractResolver resolver) => string.IsNullOrWhiteSpace(jList) ? new List<T>() : Deserialize<List<T>>(jList,resolver);
//        public static List<T> DeserializeList<T>(string jList) => string.IsNullOrWhiteSpace(jList) ? new List<T>() : Deserialize<List<T>>(jList);
//    }
//}