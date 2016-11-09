var app = angular.module("Messanger", ["ngRoute", "LocalStorageModule", 'ngSanitize', 'ui.bootstrap']);

app.config(function ($routeProvider) {

    $routeProvider.when("/home", {
        controller: "HomeController",
        templateUrl: "AngularPart/Views/Home.html"
    });

    $routeProvider.when("/login", {
        controller: "LogInController",
        templateUrl: "AngularPart/Views/LogIn.html"
    });

    $routeProvider.when("/signup", {
        controller: "SignUpController",
        templateUrl: "AngularPart/Views/SignUp.html"
    });

    $routeProvider.when("/chats", {
        controller: "ChatsController",
        templateUrl: "AngularPart/Views/Chats.html"
    });
    $routeProvider.when("/associate", {
        controller: "AssociateController",
        templateUrl: "AngularPart/Views/Association.html"
    });

    $routeProvider.otherwise({ redirectTo: "/home" });
});

app.constant('webMessengerSettings', {
    apiServiceBaseUri: '',
    clientId: 'SMARTMessenger'
});

app.config(function ($httpProvider) {
    $httpProvider.interceptors.push('AuthInterceptorService');
});

app.run(["authService", function (authService) {
    authService.fillAuthData();
}]);