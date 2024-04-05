(function () {
    "use strict";
    angular
        .module("traceRouteApp")
        .controller("traceRouteController", traceRouteController);

    function traceRouteController($http, $timeout, $scope) {
        var vm = this;
        var theResponse;
        vm.HostList = [];
        $(function () {

        });

        vm.TraceRoute = function () {
            markers = [];
            clearMarkersAndPaths();            

            var bsOffcanvas = new bootstrap.Offcanvas($("#offcanvas"));
            bsOffcanvas.show();

            $http.get("api/trace/" + vm.Hostname)
                .then(
                    function successFunction(response) {
                        theResponse = angular.fromJson(response);
                        if (theResponse.data.errorDescription)
                        {
                            bsOffcanvas.hide();
                            vm.ErrorDescription = theResponse.data.errorDescription;
                            var toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                            toastError.show();
                            return;
                        }
                        vm.HostList = theResponse.data.hops;
                        $('[data-toggle="tooltip"]').tooltip();
                        for (let i = 0; i < vm.HostList.length; i++) {
                            $http.get("api/IPInfo/" + vm.HostList[i].hopAddress)
                                .then(
                                    function successDetail(responseDetail) {
                                        vm.HostList[i].details = angular.fromJson(responseDetail).data; 
                                    }
                            );
                        }
                        autoZoom();
                    }
                );               
        };
    };
})();