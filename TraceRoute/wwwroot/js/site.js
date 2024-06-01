var map;
var polyline;
var markers = [];

function initMap() {
	map = L.map('map').setView([0, 0], 2);
	L.Icon.Default.imagePath = 'lib/leaflet/images/';
	const tiles = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
		maxZoom: 19,
		attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
	}).addTo(map);
	L.Map.include({
		getMarkerById: function (id) {
			var marker = null;
			this.eachLayer(function (layer) {
				if (layer instanceof L.Marker) {
					if (layer.options.id === id) {
						marker = layer;
					}
				}
			});
			return marker;
		}
	});
}

function addMarker(lat, long, text, ipAddress) {
	var marker = L.marker([lat, long], {id: 'marker_' + text})
		.bindTooltip(text,
			{
				permanent: true,
				direction: 'right'
			})
		.on('mouseover', hilightHopTable)
		.on('click', function (evt) { ipDetailsJS(ipAddress); })
		.addTo(map);
	markers.push[marker];
}

function ipDetailsJS(ipAddress) {
	var scope = angular.element(document.querySelector("body")).controller();
	scope.IpDetails(ipAddress);
}
function hilightHopTable() {
	var tooltip = this.getTooltip();
	var hopRowID = ".tr_hop_" + tooltip.getContent();

	$(hopRowID).addClass("highlight");
	setTimeout(function () {
		$(hopRowID).removeClass('highlight');
	}, 2000);
}

function hilightTooltip(element) {
	const index = $(element).data("index-value");
	const marker = map.getMarkerById('marker_' + index);
	if (marker != null) {
		$(marker._icon).addClass('highlight');
		setTimeout(function () {
			$(marker._icon).removeClass('highlight');
		}, 1000);
	}
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