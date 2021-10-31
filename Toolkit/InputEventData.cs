using Assets.Scripts.Tools;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public abstract class InputEventData<T>
    {
        /// <summary>
        /// The input data of the event.
        /// </summary>
        public T InputData { get; private set; }

        public InputEventData(Tool tool, T data)
        {
            Tool = tool;
            InputData = data;
        }

        public Tool Tool { get; set; }
    }

    public abstract class InputEventData
    {
        public InputEventData(Tool tool)
        {
            Tool = tool;
        }

        public Tool Tool { get; set; }
    }
}
