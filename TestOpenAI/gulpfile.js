/// <binding ProjectOpened='default' />
const gulp = require("gulp");

var vendor = [
    "./node_modules/@fortawesome/fontawesome-free/js/fontawesome.min.js",
    "./node_modules/@fortawesome/fontawesome-free/js/solid.min.js"
]

function copyVendor(cb) {
    gulp.src(vendor)
        .pipe(gulp.dest("./wwwroot/vendor/"));
    cb();
}
exports.copyVendor = copyVendor;

exports.default = function (cb) {
    gulp.watch(vendor, { ignoreInitial: false }, copyVendor);
    cb();
};