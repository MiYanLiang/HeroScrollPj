using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Utl
{
    internal class UnityMainThread : MonoBehaviour
    {
        internal static UnityMainThread thread;
        public ExceptionHandlerUi ExceptionPanel;
        private ConcurrentQueue<UnityAction> jobs = new ConcurrentQueue<UnityAction>();
        void Awake()
        {
            if(!thread)
            {
                thread = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            ExceptionPanel.Init();
        }

        void Update()
        {
            while (jobs.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    // 日志记录或其他异常处理
                    Debug.LogError($"Exception occurred during MainThreadDispatcher action: {ex}");
                }
            }
        }

        internal void RunNextFrame(UnityAction newJob)
        {
            if (newJob == null) throw new ArgumentNullException(nameof(newJob));
            jobs.Enqueue(newJob);
        }
    }
}
