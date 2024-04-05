// Write your JavaScript code.

var traceRoute;
var map;
var markers = [];
var polyLine = [];
var bounds = [];

function initMap() {
    var styleNightMode = new google.maps.StyledMapType(
        [
            { elementType: 'geometry', stylers: [{ color: '#242f3e' }] },
            { elementType: 'labels.text.stroke', stylers: [{ color: '#242f3e' }] },
            { elementType: 'labels.text.fill', stylers: [{ color: '#746855' }] },
            {
                featureType: 'administrative.locality',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#d59563' }]
            },
            {
                featureType: 'poi',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#d59563' }]
            },
            {
                featureType: 'poi.park',
                elementType: 'geometry',
                stylers: [{ color: '#263c3f' }]
            },
            {
                featureType: 'poi.park',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#6b9a76' }]
            },
            {
                featureType: 'road',
                elementType: 'geometry',
                stylers: [{ color: '#38414e' }]
            },
            {
                featureType: 'road',
                elementType: 'geometry.stroke',
                stylers: [{ color: '#212a37' }]
            },
            {
                featureType: 'road',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#9ca5b3' }]
            },
            {
                featureType: 'road.highway',
                elementType: 'geometry',
                stylers: [{ color: '#746855' }]
            },
            {
                featureType: 'road.highway',
                elementType: 'geometry.stroke',
                stylers: [{ color: '#1f2835' }]
            },
            {
                featureType: 'road.highway',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#f3d19c' }]
            },
            {
                featureType: 'transit',
                elementType: 'geometry',
                stylers: [{ color: '#2f3948' }]
            },
            {
                featureType: 'transit.station',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#d59563' }]
            },
            {
                featureType: 'water',
                elementType: 'geometry',
                stylers: [{ color: '#17263c' }]
            },
            {
                featureType: 'water',
                elementType: 'labels.text.fill',
                stylers: [{ color: '#515c6d' }]
            },
            {
                featureType: 'water',
                elementType: 'labels.text.stroke',
                stylers: [{ color: '#17263c' }]
            }
        ],
        { name: 'Night Mode' });

    map = new google.maps.Map(document.getElementById('map'), {
        center: {lat: 38, lng: -98},
        zoom: 5,
        mapTypeControlOptions: {
            mapTypeIds: ['roadmap', 'night_mode'],
            position: google.maps.ControlPosition.BOTTOM_CENTER
        }
    });

    map.mapTypes.set('night_mode', styleNightMode);
    map.setMapTypeId('roadmap');

    var lineSymbol = {
        path: google.maps.SymbolPath.FORWARD_OPEN_ARROW,
        fillColor: 'black',
        fillOpacity: .5
    };

    traceRoute = new google.maps.Polyline({
        strokeColor: '#4286f4',
        strokeOpacity: 1.0,
        strokeWeight: 3,
        icons: [{
            icon: lineSymbol,
            offset: '100%'
        }],
        map: map
    });
    traceRoute.setMap(map);

}

function addLatLng(event) {
    var path = traceRoute.getPath();

    path.push(event.latLng);

    var marker = new google.maps.Marker({
        position: event.latLng,
        title: '#' + path.getLength(),
        map: map
    });
}

function addMarker(location) {
    
    var marker = new google.maps.Marker({
        position: location,
        map: map
    });

    return marker;
} 

function clearMarkers()
{
}

function clearMarkersAndPaths()
{
    clearMarkers();
    markers = [];
    polyLine = [];
    bounds = [];
}   

function autoZoom() {
    if (bounds && bounds.length > 0) {
        map.fitBounds(bounds);
        map.panToBounds(bounds);
    }
}