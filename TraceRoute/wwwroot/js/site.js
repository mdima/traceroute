var map;

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
}

function clearMarkersAndPaths() {
	//map.clearMarkers(map)
}

function drawPath(HostList) {
	var latlngs = Array();

	for (let i = 0; i < HostList.length; i++) {
		if (HostList[i].details && HostList[i].details.latitude && HostList[i].details.longitude) {
			const point = { lat: HostList[i].details.latitude, lon: HostList[i].details.longitude };
			latlngs.push(point);
		}
	}
	var polyline = L.polyline(latlngs, { color: 'red' }).addTo(map);
	map.fitBounds(polyline.getBounds());
}