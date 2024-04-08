(function () {
    "use strict";
    angular
        .module("traceRouteApp")
        .controller("traceRouteController", traceRouteController);

    function traceRouteController($http, $timeout, $scope) {
        var vm = this;
        var theResponse;
        vm.HostList = [];
        vm.isTracing = false;
        $(function () {

        });

        vm.TraceRoute = function () {
            vm.isTracing = true;
            clearMarkersAndPaths();            
            $http.get("api/trace/" + vm.Hostname)
                .then(
                    function successFunction(response) {
                        vm.isTracing = false;
                        theResponse = angular.fromJson(response);
                        if (theResponse.data.errorDescription)
                        {
                            //alert("error")
                            vm.ErrorDescription = theResponse.data.errorDescription;
                            let toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                            toastError.show();        
                            return;
                        }
                        vm.HostList = theResponse.data.hops;
                        $('.popover-dismiss').popover({
                            trigger: 'focus'
                        })
                        for (let i = 0; i < vm.HostList.length; i++) {
                            $http.get("api/IPInfo/" + vm.HostList[i].hopAddress)
                                .then(
                                    function successDetail(responseDetail) {
                                        var hostDetail = angular.fromJson(responseDetail).data;
                                        vm.HostList[i].details = hostDetail;
                                        if (hostDetail.longitude && hostDetail.latitude) {
                                            addMarker(hostDetail.latitude, hostDetail.longitude, (i + 1).toString());
                                            drawPath(vm.HostList);
                                        }
                                    }
                            );
                        }
                    }
                );               
        };

        vm.ShowAbout = function () {
            $http.get("about/")
                .then(
                    function successFunction(response) {
                        $("#offcanvasAbout").html(response.data);
                    }
            );
        }
    };
})();