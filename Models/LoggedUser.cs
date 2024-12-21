using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Y_Platform.Models
{
    /// <summary>
    /// Klasa rozszerzająca funkcjonalność sesji użytkownika (ISession).
    /// Umożliwia przechowywanie obiektów w sesji w formie zserializowanej
    /// oraz ich późniejsze pobieranie w formie obiektów.
    /// </summary>
    public static class LoggedUser
    {
        /// <summary>
        /// Dodaje obiekt do sesji, serializując go do formatu JSON.
        /// Dzięki temu obiekt może być przechowywany jako string w sesji.
        /// </summary>
        /// <param name="session">Obiekt sesji, w którym dane mają być przechowywane.</param>
        /// <param name="key">Klucz identyfikujący obiekt w sesji.</param>
        /// <param name="value">Obiekt, który ma być zapisany w sesji.</param>
        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }
        /// <summary>
        /// Pobiera obiekt z sesji na podstawie klucza i deserializuje go z formatu JSON.
        /// </summary>
        /// <typeparam name="T">Typ obiektu, który ma zostać pobrany z sesji.</typeparam>
        /// <param name="session">Obiekt sesji, z którego dane mają być odczytane.</param>
        /// <param name="key">Klucz identyfikujący obiekt w sesji.</param>
        /// <returns>
        /// Zdeserializowany obiekt typu T, jeśli istnieje w sesji; 
        /// w przeciwnym razie wartość domyślna dla typu T (np. null dla obiektów referencyjnych).
        /// </returns>
        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
