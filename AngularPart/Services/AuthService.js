//"use strict";
app.factory("authService", ["$http", "$q", '$injector', "localStorageService", "chatsHubService", 'webMessengerSettings',
    function ($http, $q, $injector, localStorageService, chatsHubService, webMessengerSettings) {

    var serviceBase = webMessengerSettings.apiServiceBaseUri;
    //var $http;
    var authServiceFactory = {};
    var authentication = {
        isAuth: false,
        userName: ""
    };

    var externalAuthData = {
        provider: "",
        userName: "",
        externalAccessToken: ""
    };

    var logOut = function () {

        localStorageService.remove("authorizationData");

        authentication.isAuth = false;
        authentication.userName = "";

    };

    var login = function (loginData) {

        var data = "grant_type=password&username=" + loginData.userName + "&password=" +
            loginData.password + "&client_id=" + webMessengerSettings.clientId;;

        var deferred = $q.defer();

        //$http = $http || $injector.get('$http');
        $http.post(serviceBase + "/token", data,  { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } })//{ grant_type: "password", username: loginData.userName, password: loginData.password })
        .success(function (data) {
         if (data && data.access_token) {
             chatsHubService.setTokenCookie(data.access_token);
             localStorageService.set("authorizationData", {
                 token: data.access_token,
                 userName: loginData.userName,
                 refreshToken: data.refresh_token
             });

             authentication.isAuth = true;
             authentication.userName = loginData.userName;

             deferred.resolve(data);

         }
     }).error(function (err, status) {
         logOut();
         deferred.reject(err);
     });
        return deferred.promise;
    };

    var fillAuthData = function () {

        var authData = localStorageService.get("authorizationData");
        if (authData) {
            authentication.isAuth = true;
            authentication.userName = authData.userName;
        }

    }

    var saveRegistration = function (registration) {

        logOut();

        //$http = $http || $injector.get('$http');
        return $http.post(serviceBase + "api/Account/Register", registration).then(function (response) {
            return response;
        });

    };

    var refreshToken = function () {
        var deferred = $q.defer();

        var authData = localStorageService.get('authorizationData');

        if (authData) {

            var data = "grant_type=refresh_token&refresh_token=" + authData.refreshToken + "&client_id=" + webMessengerSettings.clientId;

            localStorageService.remove('authorizationData');

            //$http = $http || $injector.get('$http');
            $http.post(serviceBase + '/token', data, { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }).success(function (response) {

                localStorageService.set('authorizationData', { token: response.access_token, userName: response.userName, refreshToken: response.refresh_token});

                deferred.resolve(response);

            }).error(function (err, status) {
                logOut();
                deferred.reject(err);
            });
        } else {
            deferred.reject();
        }

        return deferred.promise;
    };

    var registerExternal = function (registerExternalData) {

        var deferred = $q.defer();

        $http.post(serviceBase + 'api/account/RegisterExternal', registerExternalData).success(function (response) {

            localStorageService.set('authorizationData', { token: response.access_token, userName: response.userName, refreshToken: "", useRefreshTokens: true });

            authentication.isAuth = true;
            authentication.userName = response.userName;
            authentication.useRefreshTokens = true;

            deferred.resolve(response);

        }).error(function (err, status) {
            logOut();
            deferred.reject(err);
        });

        return deferred.promise;

    };

    var obtainAccessToken = function (externalData) {

        var deferred = $q.defer();

        $http.get(serviceBase + 'api/account/ObtainLocalAccessToken', { params: { provider: externalData.provider, externalAccessToken: externalData.externalAccessToken } }).success(function (response) {

            localStorageService.set('authorizationData', { token: response.access_token, userName: response.userName, refreshToken: "", useRefreshTokens: false });

            authentication.isAuth = true;
            authentication.userName = response.userName;
            authentication.useRefreshTokens = false;

            deferred.resolve(response);

        }).error(function (err, status) {
            logOut();
            deferred.reject(err);
        });

        return deferred.promise;

    };

    authServiceFactory.obtainAccessToken = obtainAccessToken;
    authServiceFactory.registerExternal = registerExternal;
    authServiceFactory.saveRegistration = saveRegistration;
    authServiceFactory.login = login;
    authServiceFactory.logOut = logOut;
    authServiceFactory.fillAuthData = fillAuthData;
    authServiceFactory.authentication = authentication;
    authServiceFactory.refreshToken = refreshToken;

    return authServiceFactory;
}]);