<!DOCTYPE html>
<html>
<head>
    <title>PDF Viewer</title>
    <script src="https://mozilla.github.io/pdf.js/build/pdf.js"></script>
    <style>
        body {
            margin: 0;
            overflow: hidden;
        }
        #pdf-viewer {
            height: 100vh;
            width: 100vw;
        }
    </style>
</head>
<body>
    <canvas id="pdf-viewer"></canvas>
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            var url = 'http://www.scielo.org.co/pdf/rfdcp/v53n138/0120-3886-rfdcp-53-138-1d.pdf';
            var pdfjsLib = window['pdfjs-dist/build/pdf'];
            pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://mozilla.github.io/pdf.js/build/pdf.worker.js';

            function renderPDF(url) {
                var loadingTask = pdfjsLib.getDocument(url);
                loadingTask.promise.then(function(pdf) {
                    pdf.getPage(1).then(function(page) {
                        var scale = 1.5;
                        var viewport = page.getViewport({ scale: scale });
                        var canvas = document.getElementById('pdf-viewer');
                        var context = canvas.getContext('2d');
                        canvas.height = viewport.height;
                        canvas.width = viewport.width;

                        var renderContext = {
                            canvasContext: context,
                            viewport: viewport
                        };
                        page.render(renderContext);
                    });
                }).catch(function(error) {
                    console.error('Error loading PDF: ', error);
                });
            }

            // Renderizar el PDF después de que el DOM esté listo
            renderPDF(url);
        });
    </script>
</body>
</html>
