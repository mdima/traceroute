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
        vm.settings = new Settings();
        //Toast
        vm.toastMessage = "";
        vm.toastIsError = false;

        $(function () {
            vm.GetSettings();
        });

        vm.TraceRoute = function () {
            if (!vm.Hostname)
            {
                vm.showToaster("Please specify an IP or host to trace", true);
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
                            vm.showToaster(theResponse.data.errorDescription, true);                            
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
                    vm.showToaster("Could not calculate the route", true);
                    console.error('An error occurred:', err);
            });
        };

        vm.GetSettings = function () {
            $http.get("api/settings/")
                .then(
                    function successFunction(response) {
                        theResponse = angular.fromJson(response);
                        vm.settings = theResponse.data;                        
                    }
                )
                .catch((err) => {
                    vm.showToaster("Could not retrive the settings", true);
                    console.error('An error occurred:', err);
                });
        };

        vm.SetSettings = function () {
            $http.post("api/settings/", vm.settings)
                .then(
                    function successFunction(response) {
                        vm.showToaster("Settings saved", false);
                        vm.GetSettings();
                        $('#modalSettings').modal('hide');
                    }
                )
                .catch((err) => {
                    vm.showToaster("Could not save the settings", true);
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
                            vm.showToaster(vm.ipDetail.status, true);
                            $('#modalIpDetails').modal('hide');
                        }
                    }
                )
                .catch((err) => {
                    vm.showToaster("Could not retrive the IP information", true);
                    console.error('An error occurred:', err);
                });
        }

        vm.closeModalIpDetails = function (ipAddress) {
            vm.ipDetail = null;
            $('#modalIpDetails').modal('hide');
        }

        vm.showToaster = function (message, isError) {
            vm.toastMessage = message;
            vm.isError = isError;

            let toastDiv = bootstrap.Toast.getOrCreateInstance($("#ToastMessage"));
            toastDiv.show();
        }
    };

    class Settings {
        hostRemoteTraces = false;
        enableRemoteTraces = false;
        serverId = "";
        rootNode = "";
        currentServerURL = "";
        serverLocation = "";
    }
})();