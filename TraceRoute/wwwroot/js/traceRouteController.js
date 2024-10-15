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
        vm.ipDetail = null;
        //Setting
        vm.hostRemoteTraces = false;
        vm.enableRemoteTraces = false;
        vm.serverId = "";
        vm.rootNode = "";
        vm.currentServerURL = "";
        vm.serverLocation = "";

        $(function () {
            vm.GetSettings();
        });

        vm.TraceRoute = function () {
            if (!vm.Hostname)
            {
                vm.ErrorDescription = "Please specify an IP or host to trace";
                let toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                toastError.show();
                return;
            }
            vm.isTracing = true;
            vm.HostList = [];
            clearMarkersAndPaths();
            $http.get("api/trace/" + vm.Hostname)
                .then(
                    function successFunction(response) {
                        hideKeyboard();
                        vm.isTracing = false;
                        theResponse = angular.fromJson(response);
                        if (theResponse.data.errorDescription)
                        {
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
                                            addMarker(hostDetail.latitude, hostDetail.longitude, (i + 1).toString(), vm.HostList[i].hopAddress);
                                            drawPath(vm.HostList);
                                        }
                                        $(window).trigger('resize');
                                    }
                            );
                        }
                    }
                )
                .catch((err) => {
                    vm.isTracing = false;
                    vm.ErrorDescription = "Could not calculate the route";
                    let toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                    toastError.show();
                    console.error('An error occurred:', err);
            });
        };

        vm.GetSettings = function () {
            $http.get("api/settings/")
                .then(
                    function successFunction(response) {
                        theResponse = angular.fromJson(response);
                        vm.hostRemoteTraces = theResponse.data.hostRemoteTraces;
                        vm.enableRemoteTraces = theResponse.data.enableRemoteTraces;
                        vm.serverId = theResponse.data.serverId;
                        vm.rootNode = theResponse.data.rootNode;
                        vm.currentServerURL = theResponse.data.currentServerURL;
                        vm.ServerLocation = theResponse.data.serverLocation;
                    }
                )
                .catch((err) => {
                    vm.ErrorDescription = "Could not retrive the settings";
                    let toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                    toastError.show();
                    console.error('An error occurred:', err);
                });
        };

        vm.IpDetails = function(ipAddress)
        {
            vm.ipDetail = null;
            $('#modalIpDetails').modal('show');
            $http.get("api/IPDetails/" + ipAddress)
                .then(
                    function successFunction(response) {
                        vm.ipDetail = angular.fromJson(response).data;
                        if (vm.ipDetail.status != "success") {
                            vm.ErrorDescription = vm.ipDetail.status;
                            let toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                            toastError.show();
                            $('#modalIpDetails').modal('hide');
                        }
                    }
                )
                .catch((err) => {
                    vm.ErrorDescription = "Could not retrive the IP information";
                    let toastError = bootstrap.Toast.getOrCreateInstance($("#ToastError"));
                    toastError.show();
                    console.error('An error occurred:', err);
                });
        }

        vm.closeModalIpDetails = function (ipAddress) {
            vm.ipDetail = null;
            $('#modalIpDetails').modal('hide');
        }
    };
})();