(function () {
    "use strict";
    angular
        .module("traceRouteApp")
        .controller("traceRouteController", traceRouteController);

    function traceRouteController($http, $timeout, $scope) {
        var vm = this;

        vm.hostList = [];
        vm.serverList = [];
        vm.isTracing = false;
        vm.ipDetail = null;
        vm.sourceHost = null;
        vm.Hostname = "";
        //Setting
        vm.settings = new Settings();
        //Toast
        vm.toastMessage = "";
        vm.toastIsError = false;

        $(function () {
            vm.GetSettings();
            vm.GetServerList();
            $('#TraceSource').on('click', function (e) {
                vm.GetServerList();
            });
        });

        vm.TraceRoute = function () {
            if (!vm.Hostname)
            {
                vm.showToaster("Please specify an IP or host to trace", true);
                return;
            }
            vm.isTracing = true;
            vm.hostList = [];
            clearMarkersAndPaths();
            $http.get(vm.sourceHost.replace("Localhost", "") + "api/trace/" + vm.Hostname)
                .then(
                    function successFunction(response) {
                        hideKeyboard();
                        vm.isTracing = false;
                        var theResponse = angular.fromJson(response);
                        if (theResponse.data.errorDescription)
                        {
                            vm.showToaster(theResponse.data.errorDescription, true);                            
                            return;
                        }
                        vm.hostList = theResponse.data.hops;
                        $('.popover-dismiss').popover({
                            trigger: 'focus'
                        })
                        for (let i = 0; i < vm.hostList.length; i++) {
                            $http.get("api/IPInfo/" + vm.hostList[i].hopAddress)
                                .then(
                                    function successDetail(responseDetail) {
                                        var hostDetail = angular.fromJson(responseDetail).data;
                                        vm.hostList[i].details = hostDetail;
                                        if (hostDetail.longitude && hostDetail.latitude) {
                                            addMarker(hostDetail.latitude, hostDetail.longitude, (i + 1).toString(), vm.hostList[i].hopAddress);
                                            drawPath(vm.hostList);
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
                        vm.settings = angular.fromJson(response).data;                        
                    }
                )
                .catch((err) => {
                    vm.showToaster("Could not retrive the settings", true);
                    console.error('An error occurred:', err);
                });
        };

        vm.GetServerList = function () {
            $http.get("api/serverlist/")
                .then(
                    function successFunction(response) {
                        vm.serverList = angular.fromJson(response).data;
                        if (vm.sourceHost == null) {
                            const localSourceHost = vm.serverList.findIndex(x => x.isLocalHost == true);
                            if (localSourceHost >= 0) {                                
                                vm.sourceHost = vm.serverList[localSourceHost].url;
                            }
                        }
                    }
                )
                .catch((err) => {
                    vm.showToaster("Could not retrive the server list", true);
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

        vm.ServerDetails = function (url) {
            vm.ipDetail = null;
            var selectedServer = vm.serverList.find(x => x.url == url);            
            if (selectedServer) {
                vm.ipDetail = selectedServer;                
                $('#modalIpDetails').modal('show');
            }
        }

        vm.closeModalIpDetails = function (ipAddress) {
            vm.ipDetail = null;
            $('#modalIpDetails').modal('hide');
        }

        vm.showToaster = function (message, isError) {
            vm.toastMessage = message;
            vm.toastIsError = isError;

            let toastDiv = bootstrap.Toast.getOrCreateInstance($("#ToastMessage"));
            toastDiv.show();
        }

        vm.showOption = function (serverEntry) {
            if (serverEntry.country && serverEntry.city) {
                return serverEntry.country + " - " + serverEntry.city + " - " + serverEntry.url;
            }
            else {
                return serverEntry.url;
            }
        }
    };

    class Settings {
        hostRemoteTraces = true;
        enableRemoteTraces = true;
        rootNode = "";
        currentServerURL = "";
        serverLocation = "";
    }
})();