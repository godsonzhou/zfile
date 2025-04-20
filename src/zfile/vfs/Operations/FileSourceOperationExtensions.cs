namespace zfile
{
    /// <summary>
    /// 提供文件源操作的扩展方法
    /// </summary>
    public static class FileSourceOperationExtensions
    {
        /// <summary>
        /// 创建一个包装委托，用于处理带有 actionHandler 参数的 AskQuestion 方法
        /// </summary>
        /// <param name="operation">文件源操作</param>
        /// <returns>包装后的委托</returns>
        public static AskQuestionFunction CreateAskQuestionDelegate(this FileSourceOperation operation)
        {
            return (msg, question, possibleResponses, defaultOKResponse, defaultCancelResponse) =>
                operation.AskQuestion(msg, question, possibleResponses, defaultOKResponse, defaultCancelResponse, null);
        }
    }
}
