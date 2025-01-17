using System;
using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.Constants;
using ConnectApp.Main;
using ConnectApp.Models.ActionModel;
using ConnectApp.Models.Model;
using ConnectApp.Models.State;
using ConnectApp.Models.ViewModel;
using ConnectApp.redux.actions;
using ConnectApp.Utils;
using RSG;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.scheduler;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;

namespace ConnectApp.screens {
    public class EventOfflineDetailScreenConnector : StatelessWidget {
        public EventOfflineDetailScreenConnector(
            string eventId,
            Key key = null
        ) : base(key) {
            this.eventId = eventId;
        }

        readonly string eventId;

        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, EventDetailScreenViewModel>(
                converter: state => new EventDetailScreenViewModel {
                    eventId = this.eventId,
                    isLoggedIn = state.loginState.isLoggedIn,
                    eventDetailLoading = state.eventState.eventDetailLoading,
                    joinEventLoading = state.eventState.joinEventLoading,
                    channelId = state.eventState.channelId,
                    eventsDict = state.eventState.eventsDict
                },
                builder: (context1, viewModel, dispatcher) => {
                    var actionModel = new EventDetailScreenActionModel {
                        mainRouterPop = () => dispatcher.dispatch(new MainNavigatorPopAction()),
                        pushToLogin = () => dispatcher.dispatch(new MainNavigatorPushToAction {
                            routeName = MainNavigatorRoutes.Login
                        }),
                        openUrl = url => dispatcher.dispatch(new MainNavigatorPushToWebViewAction {
                            url = url
                        }),
                        copyText = text => dispatcher.dispatch(new CopyTextAction {text = text}),
                        startFetchEventDetail = () => dispatcher.dispatch(new StartFetchEventDetailAction()),
                        fetchEventDetail = (id, eventType) =>
                            dispatcher.dispatch<IPromise>(Actions.fetchEventDetail(id, eventType)),
                        startJoinEvent = () => dispatcher.dispatch(new StartJoinEventAction()),
                        joinEvent = id => dispatcher.dispatch<IPromise>(Actions.joinEvent(id)),
                        shareToWechat = (type, title, description, linkUrl, imageUrl) => dispatcher.dispatch<IPromise>(
                            Actions.shareToWechat(type, title, description, linkUrl, imageUrl))
                    };
                    return new EventOfflineDetailScreen(viewModel, actionModel);
                }
            );
        }
    }


    public class EventOfflineDetailScreen : StatefulWidget {
        public EventOfflineDetailScreen(
            EventDetailScreenViewModel viewModel = null,
            EventDetailScreenActionModel actionModel = null,
            Key key = null
        ) : base(key) {
            this.viewModel = viewModel;
            this.actionModel = actionModel;
        }

        public readonly EventDetailScreenViewModel viewModel;
        public readonly EventDetailScreenActionModel actionModel;

        public override State createState() {
            return new _EventOfflineDetailScreenState();
        }
    }

    class _EventOfflineDetailScreenState : State<EventOfflineDetailScreen>, TickerProvider {
        string _loginSubId;
        bool _showNavBarShadow;
        bool _isHaveTitle;
        Animation<RelativeRect> _animation;
        AnimationController _controller;
        float _titleHeight;
        static readonly GlobalKey eventTitleKey = GlobalKey.key("event-title");

        public override void initState() {
            base.initState();
            this._showNavBarShadow = true;
            this._isHaveTitle = false;
            this._titleHeight = 0.0f;
            this._controller = new AnimationController(
                duration: TimeSpan.FromMilliseconds(100),
                vsync: this
            );
            RelativeRectTween rectTween = new RelativeRectTween(
                RelativeRect.fromLTRB(0, 44, 0, 0),
                RelativeRect.fromLTRB(0, 13, 0, 0)
            );
            this._animation = rectTween.animate(this._controller);
            SchedulerBinding.instance.addPostFrameCallback(_ => {
                this.widget.actionModel.startFetchEventDetail();
                this.widget.actionModel.fetchEventDetail(this.widget.viewModel.eventId, EventType.offline);
            });
            this._loginSubId = EventBus.subscribe(EventBusConstant.login_success, args => {
                this.widget.actionModel.startFetchEventDetail();
                this.widget.actionModel.fetchEventDetail(this.widget.viewModel.eventId, EventType.offline);
            });
        }

        public override void dispose() {
            EventBus.unSubscribe(EventBusConstant.login_success, this._loginSubId);

            base.dispose();
        }

        public Ticker createTicker(TickerCallback onTick) {
            return new Ticker(onTick, () => $"created by {this}");
        }

        public override Widget build(BuildContext context) {
            var eventObj = new IEvent();
            if (this.widget.viewModel.eventsDict.ContainsKey(this.widget.viewModel.eventId)) {
                eventObj = this.widget.viewModel.eventsDict[this.widget.viewModel.eventId];
            }

            if ((this.widget.viewModel.eventDetailLoading || eventObj?.user == null) && !eventObj.isNotFirst) {
                return new EventDetailLoading(mainRouterPop: this.widget.actionModel.mainRouterPop);
            }

            var eventStatus = DateConvert.GetEventStatus(eventObj.begin);
            return new Container(
                color: CColors.White,
                child: new CustomSafeArea(
                    child: new Container(
                        color: CColors.White,
                        child: new NotificationListener<ScrollNotification>(
                            onNotification: notification => this._onNotification(context, notification),
                            child: new Column(
                                children: new List<Widget> {
                                    this._buildEventDetail(eventObj),
                                    this._buildOfflineRegisterNow(eventObj, this.widget.viewModel.isLoggedIn,
                                        eventStatus)
                                }
                            )
                        )
                    )
                )
            );
        }

        Widget _buildEventDetail(IEvent eventObj) {
            return new Expanded(
                child: new Stack(
                    children: new List<Widget> {
                        new EventDetail(
                            true,
                            eventObj,
                            this.widget.actionModel.openUrl,
                            titleKey: eventTitleKey
                        ),
                        new Positioned(
                            left: 0,
                            top: 0,
                            right: 0,
                            child: this._buildHeadTop(eventObj)
                        )
                    }
                )
            );
        }

        bool _onNotification(BuildContext context, ScrollNotification notification) {
            var pixels = notification.metrics.pixels;
            if (this._titleHeight == 0.0f) {
                var width = MediaQuery.of(context).size.width;
                var imageHeight = 9.0f / 16.0f * width;
                this._titleHeight = imageHeight + eventTitleKey.currentContext.size.height - 32;
            }

            if (pixels >= 44) {
                if (this._showNavBarShadow) {
                    this.setState(() => { this._showNavBarShadow = false; });
                }
            }
            else {
                if (!this._showNavBarShadow) {
                    this.setState(() => { this._showNavBarShadow = true; });
                }
            }

            if (pixels > this._titleHeight) {
                if (!this._isHaveTitle) {
                    this._controller.forward();
                    this.setState(() => { this._isHaveTitle = true; });
                }
            }
            else {
                if (this._isHaveTitle) {
                    this._controller.reverse();
                    this.setState(() => { this._isHaveTitle = false; });
                }
            }

            return true;
        }

        Widget _buildHeadTop(IEvent eventObj) {
            Widget shareWidget = new CustomButton(
                onPressed: () => ShareUtils.showShareView(new ShareView(
                    projectType: ProjectType.iEvent,
                    onPressed: type => {
                        AnalyticsManager.ClickShare(type, "Event", "Event_" + eventObj.id, eventObj.title);

                        var linkUrl =
                            $"{Config.apiAddress}/events/{eventObj.id}";
                        if (type == ShareType.clipBoard) {
                            this.widget.actionModel.copyText(linkUrl);
                            CustomDialogUtils.showToast("复制链接成功", Icons.check_circle_outline);
                        }
                        else {
                            var imageUrl = $"{eventObj.avatar}.200x0x1.jpg";
                            CustomDialogUtils.showCustomDialog(
                                child: new CustomLoadingDialog()
                            );
                            this.widget.actionModel.shareToWechat(type, eventObj.title, eventObj.shortDescription,
                                    linkUrl,
                                    imageUrl).Then(CustomDialogUtils.hiddenCustomDialog)
                                .Catch(_ => CustomDialogUtils.hiddenCustomDialog());
                        }
                    })),
                child: new Container(
                    color: CColors.Transparent,
                    child: new Icon(Icons.share, size: 28,
                        color: this._showNavBarShadow ? CColors.White : CColors.Icon))
            );

            Widget titleWidget = new Container();
            if (this._isHaveTitle) {
                titleWidget = new Text(
                    eventObj.title,
                    style: CTextStyle.PXLargeMedium,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    textAlign: TextAlign.center
                );
            }

            return new AnimatedContainer(
                height: 44,
                duration: TimeSpan.Zero,
                padding: EdgeInsets.symmetric(horizontal: 8),
                decoration: new BoxDecoration(
                    CColors.White,
                    border: new Border(
                        bottom: new BorderSide(this._isHaveTitle ? CColors.Separator2 : CColors.Transparent)),
                    gradient: this._showNavBarShadow
                        ? new LinearGradient(
                            colors: new List<Color> {
                                new Color(0x80000000),
                                new Color(0x0)
                            },
                            begin: Alignment.topCenter,
                            end: Alignment.bottomCenter
                        )
                        : null
                ),
                child: new Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: new List<Widget> {
                        new CustomButton(
                            onPressed: () => this.widget.actionModel.mainRouterPop(),
                            child: new Icon(
                                Icons.arrow_back,
                                size: 28,
                                color: this._showNavBarShadow ? CColors.White : CColors.Icon
                            )
                        ),
                        new Expanded(
                            child: new Stack(
                                fit: StackFit.expand,
                                children: new List<Widget> {
                                    new PositionedTransition(
                                        rect: this._animation,
                                        child: titleWidget
                                    )
                                }
                            )
                        ),
                        shareWidget
                    }
                )
            );
        }

        Widget _buildOfflineRegisterNow(IEvent eventObj, bool isLoggedIn, EventStatus eventStatus) {
            if (eventObj.type.isNotEmpty() && !(eventObj.type == "bagevent" || eventObj.type == "customize")) {
                return new Container();
            }

            var buttonText = "立即报名";
            var backgroundColor = CColors.PrimaryBlue;
            var isEnabled = true;

            if (eventStatus == EventStatus.past) {
                buttonText = "已结束";
                backgroundColor = CColors.Disable;
                isEnabled = false;
            }

            return new Container(
                height: 64,
                padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                decoration: new BoxDecoration(
                    CColors.White,
                    border: new Border(new BorderSide(CColors.Separator))
                ),
                child: new CustomButton(
                    onPressed: () => {
                        if (!isEnabled) {
                            return;
                        }

                        if (isLoggedIn && eventObj.type.isNotEmpty()) {
                            if (eventObj.type == "bagevent") {
                                this.widget.actionModel.openUrl(
                                    $"{Config.apiAddress}/events/{eventObj.id}/purchase");
                            }
                            else if (eventObj.type == "customize" && eventObj.typeParam.isNotEmpty()) {
                                this.widget.actionModel.openUrl(eventObj.typeParam);
                            }
                        }
                        else {
                            this.widget.actionModel.pushToLogin();
                        }
                    },
                    padding: EdgeInsets.zero,
                    child: new Container(
                        decoration: new BoxDecoration(
                            backgroundColor,
                            borderRadius: BorderRadius.all(4)
                        ),
                        child: new Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: new List<Widget> {
                                new Text(
                                    buttonText,
                                    style: CTextStyle.PLargeMediumWhite
                                )
                            }
                        )
                    )
                )
            );
        }
    }
}