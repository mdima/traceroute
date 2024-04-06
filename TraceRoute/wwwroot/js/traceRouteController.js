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

            $http.get("api/trace/" + vm.Hostname)
                .then(
                    function successFunction(response) {
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
                                            var position = { lat: hostDetail.latitude, lng: hostDetail.longitude };
                                            var marker = addMarker(position, (i + 1));
                                            markers.push(marker);
                                            autoZoom();
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