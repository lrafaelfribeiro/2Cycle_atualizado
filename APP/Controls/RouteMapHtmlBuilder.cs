using System.Globalization;
using System.Text.Json;

namespace APP.Controls
{
    public static class RouteMapHtmlBuilder
    {
        private static async Task<string> ReadRawFileAsync(string filename)
        {
            using var stream =
                await FileSystem.OpenAppPackageFileAsync(filename);

            using var reader =
                new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }

        public static async Task<string> BuildStaticRouteHtml(
            IReadOnlyList<(double Latitude, double Longitude)> points,
            string lineColor = "#A6E22E",
            string startColor = "#A6E22E")
        {
            if (points.Count < 2)
            {
                return """
                <html>
                <body style="background:#0D0D0F"></body>
                </html>
                """;
            }

            // =============================================================
            // COORDENADAS
            // =============================================================

            var coordsJson = JsonSerializer.Serialize(
                points.Select(p => new[]
                {
                    p.Longitude,
                    p.Latitude
                }));

            var first = points[0];

            // =============================================================
            // MAPLIBRE
            // =============================================================

            var mapLibreJs =
                await ReadRawFileAsync("maplibre-gl.js");

            var mapLibreCss =
                await ReadRawFileAsync("maplibre-gl.css");

            // =============================================================
            // HTML
            // =============================================================

            return $$"""
            <!DOCTYPE html>

            <html>

            <head>

                <meta
                    name="viewport"
                    content="width=device-width, initial-scale=1.0"/>

                <style>
                    {{mapLibreCss}}
                </style>

                <script>
                    {{mapLibreJs}}
                </script>

                <style>

                    html,
                    body,
                    #map {

                        width: 100%;
                        height: 100%;

                        margin: 0;
                        padding: 0;

                        background: #0D0D0F;
                    }


                    /* =====================================================
                       CONTROLOS MAPLIBRE
                       ===================================================== */

                    .maplibregl-ctrl-logo {

                        display: none !important;
                    }


                    /* =====================================================
                       FINISH FLAG
                       ===================================================== */

                    .route-end-flag {

                        width: 32px;
                        height: 32px;

                        display: flex;

                        align-items: center;
                        justify-content: center;

                        font-size: 26px;
                        line-height: 1;

                        filter:
                            drop-shadow(
                                0 1px 2px rgba(0,0,0,.7)
                            );
                    }


                    /* =====================================================
                       MAP TOGGLE
                       ===================================================== */

                    .map-toggle {

                        position: absolute;

                        top: 12px;
                        right: 12px;

                        z-index: 10;

                        display: flex;

                        gap: 6px;

                        background:
                            rgba(13,13,15,0.85);

                        padding: 4px;

                        border-radius: 10px;

                        box-shadow:
                            0 2px 8px rgba(0,0,0,.25);
                    }


                    .map-toggle button {

                        border: none;
                        outline: none;

                        cursor: pointer;

                        padding: 6px 12px;

                        border-radius: 8px;

                        font-size: 13px;
                        font-weight: 600;

                        font-family:
                            -apple-system,
                            BlinkMacSystemFont,
                            "Segoe UI",
                            sans-serif;

                        background: transparent;

                        color:
                            rgba(255,255,255,0.6);

                        transition:
                            background .15s,
                            color .15s;
                    }


                    .map-toggle button.active {

                        background: #A6E22E;

                        color: #0D0D0F;
                    }


                    /* =====================================================
                       ATRIBUIÇÃO
                       ===================================================== */

                    .maplibregl-ctrl-attrib {

                        font-size: 9px !important;

                        background:
                            rgba(13,13,15,0.75) !important;

                        color:
                            rgba(255,255,255,0.75) !important;
                    }


                    .maplibregl-ctrl-attrib a {

                        color:
                            rgba(255,255,255,0.85) !important;
                    }

                </style>

            </head>


            <body>

                <div id="map"></div>


                <div class="map-toggle">

                    <button
                        id="btn-mapa"
                        class="active">

                        Mapa

                    </button>


                    <button
                        id="btn-satelite">

                        Satélite

                    </button>

                </div>


                <script>

                    // =====================================================
                    // DADOS DA ROTA
                    // =====================================================

                    var routeCoords =
                        {{coordsJson}};


                    // =====================================================
                    // MAPA
                    // =====================================================

                    var map =
                        new maplibregl.Map({

                            container: 'map',

                            /*
                             * Mapa base OpenFreeMap.
                             *
                             * Não precisa de API key.
                             */

                            style:
                                'https://tiles.openfreemap.org/styles/dark',

                            center: [

                                {{first.Longitude.ToString(CultureInfo.InvariantCulture)}},

                                {{first.Latitude.ToString(CultureInfo.InvariantCulture)}}

                            ],

                            zoom: 12,

                            minZoom: 5,

                            maxZoom: 19,

                            pitch: 0,

                            bearing: 0,

                            attributionControl: true

                        });


                    // =====================================================
                    // MAP LOAD
                    // =====================================================

                    map.on('load', function () {


                        // =================================================
                        // SATÉLITE
                        // ESRI WORLD IMAGERY
                        // =================================================

                        map.addSource(
                            'satellite',
                            {

                                type: 'raster',

                                tiles: [

                                    'https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'

                                ],

                                tileSize: 256,

                                minzoom: 0,

                                maxzoom: 19,

                                attribution:
                                    '© Esri, Maxar, Earthstar Geographics, and the GIS User Community'

                            }
                        );


                        // =================================================
                        // SATELLITE LAYER
                        // =================================================

                        map.addLayer(
                            {

                                id:
                                    'satellite-layer',

                                type:
                                    'raster',

                                source:
                                    'satellite',

                                layout:
                                    {

                                        /*
                                         * Começa escondida.
                                         */

                                        visibility:
                                            'none'

                                    },

                                paint:
                                    {

                                        'raster-opacity':
                                            1,

                                        'raster-fade-duration':
                                            0

                                    }

                            }
                        );


                        // =================================================
                        // ROUTE SOURCE
                        // =================================================

                        map.addSource(
                            'route',
                            {

                                type:
                                    'geojson',

                                data:
                                    {

                                        type:
                                            'Feature',

                                        properties:
                                            {},

                                        geometry:
                                            {

                                                type:
                                                    'LineString',

                                                coordinates:
                                                    routeCoords

                                            }

                                    }

                            }
                        );


                        // =================================================
                        // ROUTE LINE
                        // =================================================

                        map.addLayer(
                            {

                                id:
                                    'route-line',

                                type:
                                    'line',

                                source:
                                    'route',

                                layout:
                                    {

                                        'line-join':
                                            'round',

                                        'line-cap':
                                            'round'

                                    },

                                paint:
                                    {

                                        'line-color':
                                            '{{lineColor}}',

                                        'line-width':
                                            5,

                                        'line-opacity':
                                            1

                                    }

                            }
                        );


                        // =================================================
                        // GARANTIR ROTA POR CIMA DO SATÉLITE
                        // =================================================

                        map.moveLayer(
                            'route-line'
                        );


                        // =================================================
                        // START MARKER
                        // =================================================

                        new maplibregl.Marker(
                            {

                                color:
                                    '{{startColor}}'

                            }
                        )

                        .setLngLat(
                            routeCoords[0]
                        )

                        .addTo(
                            map
                        );


                        // =================================================
                        // FINISH MARKER
                        // =================================================

                        var flagEl =
                            document.createElement(
                                'div'
                            );


                        flagEl.className =
                            'route-end-flag';


                        flagEl.textContent =
                            '🏁';


                        new maplibregl.Marker(
                            {

                                element:
                                    flagEl,

                                anchor:
                                    'bottom-left'

                            }
                        )

                        .setLngLat(
                            routeCoords[
                                routeCoords.length - 1
                            ]
                        )

                        .addTo(
                            map
                        );


                        // =================================================
                        // ROUTE BOUNDS
                        // =================================================

                        var bounds =
                            routeCoords.reduce(

                                function (
                                    b,
                                    coord
                                ) {

                                    return b.extend(
                                        coord
                                    );

                                },

                                new maplibregl.LngLatBounds(
                                    routeCoords[0],
                                    routeCoords[0]
                                )

                            );


                        // =================================================
                        // FIT ROUTE
                        // =================================================

                        map.fitBounds(

                            bounds,

                            {

                                padding:
                                    60,

                                animate:
                                    true

                            }

                        );


                        // =================================================
                        // GARANTIR ROTA NOVAMENTE
                        // =================================================

                        map.moveLayer(
                            'route-line'
                        );

                    });


                    // =====================================================
                    // DEBUG MAP
                    // =====================================================

                    map.on(
                        'error',
                        function (e) {

                            console.error(
                                'MAP ERROR:',
                                e.error
                            );

                        }
                    );


                    // =====================================================
                    // BOTÕES
                    // =====================================================

                    var btnMapa =
                        document.getElementById(
                            'btn-mapa'
                        );


                    var btnSatelite =
                        document.getElementById(
                            'btn-satelite'
                        );


                    // =====================================================
                    // MAPA / SATÉLITE
                    // =====================================================

                    function setSatelite(
                        ativo
                    ) {

                        map.setLayoutProperty(

                            'satellite-layer',

                            'visibility',

                            ativo
                                ? 'visible'
                                : 'none'

                        );


                        btnMapa.classList.toggle(

                            'active',

                            !ativo

                        );


                        btnSatelite.classList.toggle(

                            'active',

                            ativo

                        );


                        /*
                         * Garante que a rota continua
                         * acima da imagem de satélite.
                         */

                        if (
                            map.getLayer(
                                'route-line'
                            )
                        ) {

                            map.moveLayer(
                                'route-line'
                            );

                        }

                    }


                    // =====================================================
                    // BOTÃO MAPA
                    // =====================================================

                    btnMapa.addEventListener(
                        'click',
                        function () {

                            setSatelite(
                                false
                            );

                        }
                    );


                    // =====================================================
                    // BOTÃO SATÉLITE
                    // =====================================================

                    btnSatelite.addEventListener(
                        'click',
                        function () {

                            setSatelite(
                                true
                            );

                        }
                    );


                    // =====================================================
                    // ESTADO INICIAL
                    // =====================================================

                    setSatelite(
                        false
                    );

                </script>

            </body>

            </html>
            """;
        }
    }
}