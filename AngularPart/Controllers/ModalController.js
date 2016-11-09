app.controller('ModalController', ['$scope', '$uibModal', '$log', 'userService',
    function ($scope, $uibModal, $log, userService) {
        var $ctrl = this;
        $ctrl.userName = $scope.$parent.currentUserName;
    $ctrl.animationsEnabled = true;
    $ctrl.code = 'enter your code here';
    /*$scope.codeChanged = function() {
        chatsService.setCode($scope.code);
    }*/

    $ctrl.open = function (size) {
        var modalInstance = $uibModal.open({
            animation: $ctrl.animationsEnabled,
            ariaLabelledBy: 'modal-title',
            ariaDescribedBy: 'modal-body',
            templateUrl: 'myModalContent.html',
            controller: 'ModalInstanceCtrl',
            controllerAs: '$ctrl',
            size: size,
            resolve: {
                userName: function () {
                    return $ctrl.userName;
                }
            }
        });

        modalInstance.result.then(function (userName) {
            $ctrl.userName = userName;
        }, function () {
            $log.info('Modal dismissed at: ' + new Date());
        });
    };

    $ctrl.openComponentModal = function () {
        var modalInstance = $uibModal.open({
            animation: $ctrl.animationsEnabled,
            component: 'modalComponent',
            resolve: {
                code: function () {
                    return $ctrl.code;
                }
            }
        });

        modalInstance.result.then(function (userName) {
            $ctrl.userName = userName;
        }, function () {
            $log.info('modal-component dismissed at: ' + new Date());
        });
    };

    $ctrl.toggleAnimation = function () {
        $ctrl.animationsEnabled = !$ctrl.animationsEnabled;
    };
}]);

// Please note that $uibModalInstance represents a modal window (instance) dependency.
// It is not the same as the $uibModal service used above.

app.controller('ModalInstanceCtrl', ['$uibModalInstance', 'userName', 'userService',
    function ($uibModalInstance, userName, userService) {
        var $ctrl = this;
        $ctrl.userName = userName;

    $ctrl.getUserInfo = function(name) {
        var data = userService.getUserByName(name);
        $ctrl.email = data.email;
        //$ctrl.img.Src = data.img;
    }

    $ctrl.ok = function () {
        $uibModalInstance.close($ctrl.userName);
    };

    $ctrl.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };
}]);

app.component('modalComponent', {
    templateUrl: 'Modal.html',
    bindings: {
        resolve: '<',
        close: '&',
        dismiss: '&'
    },
    controller: function() {
        var $ctrl = this;

        $ctrl.$onInit = function() {
            $ctrl.userName = $ctrl.resolve.userName;
        };

        $ctrl.ok = function() {
            $ctrl.close({ $value: $ctrl.userName });
        };

        $ctrl.cancel = function() {
            $ctrl.dismiss({ $value: 'cancel' });
        };
    }
});
// Please note that the close and dismiss bindings are from $uibModalInstance.
