using System;
using ConnectApp.Models.State;
using RSG;

namespace ConnectApp.Models.ActionModel {
    public class EventsScreenActionModel : BaseActionModel {
        public Action<string, EventType> pushToEventDetail;
        public Action startFetchEventOngoing;
        public Action startFetchEventCompleted;
        public Func<int, string, string, IPromise> fetchEvents;
    }
}