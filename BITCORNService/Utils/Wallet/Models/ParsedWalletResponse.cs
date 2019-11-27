namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Wrapper response object for all wallet calls;
    /// If call was succesfull (IsError is false), m_Content will be of type T, otherwise m_Content will be of type WalletError
    /// </summary>
    /// <typeparam name="T">Succesfull response datatype</typeparam>
    public class ParsedWalletResponse<T>
    {
        /// <summary>
        /// data container
        /// </summary>
        object _content = null;
        /// <summary>
        /// has this object been initialized as error?
        /// </summary>
        public bool IsError { get; private set; }
        /// <summary>
        /// has this object been initialized?
        /// </summary>
        public bool IsInitialized { get; private set; }
        /// <summary>
        /// get error object from the data container
        /// </summary>
        /// <returns>Wallet error object</returns>
        public WalletError GetError()
        {
            return GetContentInternal<WalletError>();
        }
        /// <summary>
        /// get succesfully parsed content, will return null if the data container is an error
        /// </summary>
        /// <returns>data container as T</returns>
        public T GetParsedContent()
        {
            return GetContentInternal<T>();
        }
        /// <summary>
        /// Tries to cast data container to type
        /// </summary>
        /// <returns>data container as TContent</returns>
        TContent GetContentInternal<TContent>()
        {

            if (_content is TContent)
            {
                return (TContent)_content;
            }
            return default(TContent);

        }
        /// <summary>
        /// Set data container object to succesfully parsed value
        /// </summary>
        /// <returns>Response wrapper with valid data</returns>
        public static ParsedWalletResponse<T> CreateContent(T data)
        {
            var obj = new ParsedWalletResponse<T>();
            obj.SetContent(data);
            return obj;
        }
        /// <summary>
        /// Set data container object to error
        /// </summary>
        /// <returns>Response wrapper with error</returns>
        public static ParsedWalletResponse<T> CreateError(WalletError error)
        {
            var obj = new ParsedWalletResponse<T>();
            obj.IsError = true;
            obj.SetContent(error);
            return obj;

        }
        /// <summary>
        /// Sets internal data container object
        /// </summary>
        public void SetContent(object data)
        {
            this._content = data;
            this.IsInitialized = true;
        }

    }

}
