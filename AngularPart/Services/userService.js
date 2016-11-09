'use strict';
app.factory('userService', ['$location', '$http', 'localStorageService', '$q',
    function ($location, $http, localStorageService, $q) {

    var userServiceFactory = {};
    var loginData = {};

    userServiceFactory.getUserByName = function (name) {
        var deffered = $q.deffer;
        if (name != null) {
            var uuid = guid();
            $http.get("api/User/" + name + "/" + uuid).then(function(result) {
                deffered.resolve(result);
            });
        } else ($location.path('/login'));
        return deffered.promise;
    }


    return userServiceFactory;

    function guid() {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
              .toString(16)
              .substring(1);
        }
        return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
          s4() + '-' + s4() + s4() + s4();
    }
}]);