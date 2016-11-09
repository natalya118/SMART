'use strict';
app.factory('chatsHubService', ['$http', 'localStorageService', function ($http, localStorageService) {
    var chatsHub = {};
    chatsHub.messages = [];
    var signalRChatHub = {};


    chatsHub.start = function () {
        signalRChatHub = $.connection.messageHub;
        signalRChatHub.client.onMessage = chatsHub.onMessage;
        signalRChatHub.client.connected = function () {

        }
    }

    chatsHub.setTokenCookie = function (token) {
        if (token) {
            document.cookie = "BearerToken=" + token + "; path=/";
        }
    }


    chatsHub.sendMessage = function (message) {
        var loginData = localStorageService.get("authorizationData");
        message.user = {};
        message.user.userName = loginData.userName;
        signalRChatHub.server.sendMessage(message);
    };
    chatsHub.onMessage = function (message) {
        chatsHub.messageCallback(message);
    };


    return chatsHub;
}
])