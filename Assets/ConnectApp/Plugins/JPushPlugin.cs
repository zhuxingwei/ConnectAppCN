using System.Collections.Generic;
using System.Runtime.InteropServices;
using ConnectApp.redux;
using ConnectApp.redux.actions;
using Unity.UIWidgets.engine;
using Unity.UIWidgets.external.simplejson;
using Unity.UIWidgets.widgets;
using UnityEngine;
using EventType = ConnectApp.models.EventType;

namespace ConnectApp.plugins
{
    public static class JPushPlugin
    {
        public static BuildContext context;
        public static bool isListen;
        public static void addListener()
        {
            if(!isListen) {
                UIWidgetsMessageManager.instance.AddChannelMessageDelegate("jpush", _handleMethodCall);
                completed();
                isListen = true;
            }
        }
        private static void _handleMethodCall(string method, List<JSONNode> args) {
            if (context!=null)
            {
                using (WindowProvider.of(context).getScope())
                {
                    switch (method) {
                        case "OnOpenNotification": {
                            var node = args[0];
                            var dict = JSON.Parse(node);
                            var type = dict["type"];
                            var id = dict["id"];
                            if (type == "article")
                            {
                                StoreProvider.store.dispatcher.dispatch(
                                    new MainNavigatorPushToArticleDetailAction{articleId = id});
                            }
                            else if (type == "event")
                            {
                                var eventType = EventType.offline;
                                if (dict["eventType"]=="online")
                                {
                                    eventType = EventType.online;
                                }
                                StoreProvider.store.dispatcher.dispatch(
                                    new MainNavigatorPushToEventDetailAction{eventId = id,eventType = eventType});  
                            }
                        }
                            break;
                    }
                }
            }
        }

        private static void completed()
        {
            listenCompleted();
        }

#if UNITY_IOS
        [DllImport("__Internal")]
        internal static extern void listenCompleted();

#elif UNITY_ANDROID
        
        static void listenCompleted() {
            using (
                AndroidJavaClass managerClass = new AndroidJavaClass("com.unity3d.unityconnect.plugins.JPushPlugin")
            ) {
                using (
                    AndroidJavaObject managerInstance = managerClass.CallStatic<AndroidJavaObject>("getInstance")
                ) {
                    managerInstance.Call("listenCompleted");
                }
            }
        }
#else
        public void listenCompleted() {}
#endif
    }
}