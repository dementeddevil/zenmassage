﻿// To debug code on page load in Ripple or on Android devices/emulators: launch your app, set breakpoints, 
// and then run "window.location.reload()" in the JavaScript Console.
module ZenMassageApp {
    "use strict";

    /*export module Application {
        export function initialize() {
            document.addEventListener('deviceready', onDeviceReady, false);
        }

        function onDeviceReady() {
            // Handle the Cordova pause and resume events
            document.addEventListener('pause', onPause, false);
            document.addEventListener('resume', onResume, false);

            // TODO: Cordova has been loaded. Perform any initialization that requires Cordova here.
            var pebble: Pebble = cordova.require('cordova-pebble.Pebble');
            if (typeof pebble !== 'undefined' && pebble !== null) {
                pebble.setAppUUID(
                    '29207e29-1f35-4f89-9871-0a579e84d105',
                    (info): void => {
                        navigator.notification.alert(
                            'watch app linked',
                            () => { });
                    },
                    (error): void => {
                        navigator.notification.alert(
                            'watch app not linked',
                            () => { });
                    });
            }
        }

        function onPause() {
            // TODO: This application has been suspended. Save application state here.
        }

        function onResume() {
            // TODO: This application has been reactivated. Restore application state here.
        }

    }

    window.onload = function () {
        Application.initialize();
    }*/
}
