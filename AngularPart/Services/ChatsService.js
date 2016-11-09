'use strict';
app.factory('chatsService', ['$location', '$http', 'localStorageService','$q', function ($location, $http, localStorageService,$q) {

    var chatsServiceFactory = {};
    var loginData = {};

    chatsServiceFactory.getChats = function () {
        var deferred = $q.defer();
        loginData = localStorageService.get("authorizationData");
        if (loginData != null) {
            var uuid = guid();
            $http.get("api/Chats/" + loginData.userName + "/" + uuid).then(function(results) {
                //$http.get("api/Chats").then(function (results) {
                deferred.resolve(results);
            });
        } else($location.path('/login'));
        return deferred.promise;
    };

    chatsServiceFactory.addChat = function (title, isPrivate) {
        var deferred = $q.defer();
        var newChat = {};
        newChat.title = title;
        newChat.isPrivate = isPrivate;
        newChat.authorName = loginData.userName;

        $.post("api/Chats/", newChat)
            .done(function (result) {
                deferred.resolve(result);
            });
        return deferred.promise;
    };

    chatsServiceFactory.addUserToChat = function (chatTitle, userName) {
        var deferred = $q.defer();
        var obj = {};
        obj.chatTitle = chatTitle;
        obj.userName = userName;

        $.post("api/User/", obj)
            .done(function () {
                deferred.resolve();
            });
        return deferred.promise;
    };

    chatsServiceFactory.getMessagesForChat = function (chatId) {
        var deferred = $q.defer();

        var uuid = guid();
        $.get("api/Messages/" + chatId, uuid).then(function (results) {
            deferred.resolve(results);
        });
        return deferred.promise;
    };

    function guid() {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
              .toString(16)
              .substring(1);
        }
        return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
          s4() + '-' + s4() + s4() + s4();
    }

    //for code insertion part. Not used right now, but may be useful in case of usage of modal window
    var currCode;

    var setCode = function (code) {
        currCode = code;
    }

    var getCode = function() {
        return currCode;
    }

    chatsServiceFactory.setCode = setCode;
    chatsServiceFactory.getCode = getCode;

    return chatsServiceFactory;

}]);