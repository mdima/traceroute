var map;
var polyline;
var markers = [];

$(function () {
	initMap();
});

function initMap() {
	map = L.map('map').setView([0, 0], 2);
	L.Icon.Default.imagePath = 'lib/leaflet/images/';
	const tiles = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
		maxZoom: 19,
		attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
	}).addTo(map);
}

function addMarker(lat, long, text) {
	var marker = L.marker([lat, long])
		.bindTooltip(text,
			{
				permanent: true,
				direction: 'right'
			})
		.addTo(map);
	markers.push[marker];
}

function clearMarkersAndPaths() {
	map.eachLayer((layer) => {
		if (layer['_latlng'] != undefined || layer['_path'] != undefined)
			layer.remove();
	});
	markers = [];
	polyline = undefined;
}

function drawPath(HostList) {
	var latlngs = [];

	//I want to be sure all the details have been received before drawing the line
	if (HostList.find(x => x.details == undefined || x.details.isp == undefined) != undefined) { return };
	for (let i = 0; i < HostList.length; i++) {
		if (HostList[i].details && HostList[i].details.latitude && HostList[i].details.longitude) {
			var point = new L.latLng(HostList[i].details.latitude, HostList[i].details.longitude);
			latlngs.push(point);
		}
	}
	polyline = L.polyline(latlngs, { color: 'red' }).addTo(map);
	map.fitBounds(polyline.getBounds());
}