
// Init Bootstrap tooltips and popovers:
$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
    $('[data-toggle="popover"]').popover();
});

window.onload = function (e) {
    initLibrary();
    overwolf.games.onGameInfoUpdated.addListener(onGameInfoUpdate);
};

var sampleLibraryObj = null;

function plugin() {
    return document.querySelector('#plugin');
}

function reloadPage() {
    location = location;
}

function genericCallback(result) {
    console.log("genericCallback: " + result);
};

function addImageToBar(path) {
    addImageToBarWithTooltip("playerSide", path, "No info.", "cardThumbnail");
};

function addImageToBarWithTooltip(elementName, path, tooltipTitle, id) {
    var $newdiv = $('<div class="thumbnail" data-toggle="tooltip" data-placement="bottom"/>');
    $newdiv.attr('title', tooltipTitle);
    $newdiv.attr('id', id);

    var $newimg = $('<img class="img-responsive"/>');
    $newimg.attr('src', path);

    // Append img to div:
    $newdiv.append($newimg);
    // Append div to element:
    $('#' + elementName).prepend($newdiv);
    // Enable tooltip:
    $('[data-toggle="tooltip"]').tooltip();
};

function addImageToBarWithPopover(elementName, path, tooltipTitle, popoverContent, id) {
    // Create new div:
    var $newdiv = $('<div class="thumbnail"/>');
    $newdiv.attr('title', tooltipTitle);
    $newdiv.attr('id', id);

    // Create new img and add popover:
    var $newimg = $('<img class="img-responsive"/>');
    $newimg.attr('src', path);
    $newimg.popover({ title: tooltipTitle, content: popoverContent, html: true, placement: "right", trigger: "hover" });

    // Append img to div and div to element:
    $newdiv.append($newimg);
    $('#' + elementName).prepend($newdiv);

    // Enable popover:
    $('.popover').popover({
        container: 'body'
    });
}


function dragResize(edge) {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success") {
            overwolf.windows.dragResize(result.window.id, edge);
        }
    });
};

function dragMove() {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success") {
            overwolf.windows.dragMove(result.window.id);
        }
    });
};

function closeWindow() {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success") {
            overwolf.windows.close(result.window.id);
        }
    });
};

function takeScreenshot() {
    overwolf.media.takeScreenshot(function (result) {
        if (result.status == "success") {
            var imagePath = result.url;
            console.log(result.url);
            addImageToBar(result.url);
        }
    });
};

function initRunningState() {
    // Start worker thread and change resolution if game is running:
    overwolf.games.getRunningGameInfo(function (gameInfoOBject) {
        if (gameInfoOBject != null) {
            console.log("Game is running.");
            sampleLibraryObj.StartWorkerThread(genericCallback);
            resizeWindow(gameInfoOBject.width, gameInfoOBject.height);
        }
        else {
            console.log("Game is NOT running.");
            sampleLibraryObj.StopWorkerThread(genericCallback);
        }
    });
}

function initLibrary() {
    if (typeof (overwolf) != "undefined") {
        overwolf.games.getRunningGameInfo(function (gameInfoOBject) {
            overwolf.extensions.current.getExtraObject("NinjaLibrary", function (result) {
                console.log("startup " + result.status);
                if (result.status == "success") {
                    sampleLibraryObj = result.object;

                    // Init C# module and register event handlers:
                    sampleLibraryObj.CardReceivedEvent.addListener(onCardReceived);
                    sampleLibraryObj.CardPlayedEvent.addListener(onCardPlayed);
                    sampleLibraryObj.OpponentCardPlayedEvent.addListener(onOpponentCardPlayed)
                    sampleLibraryObj.Init(genericCallback);
                    // Start worker thread incase the game is already running:
                    initRunningState();
                }
            });
        });
    }
};

function openWindow(windowName) {
    overwolf.windows.obtainDeclaredWindow(windowName, function (result) {
        if (result.status == "success") {
            overwolf.windows.restore(result.window.id, function (result) {
                console.log(result);
            });
        }
    });
};

function resizeWindow(width, height) {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success")
        {
            overwolf.windows.changeSize(result.window.id, width, height, genericCallback);
            $('#body').height(height);
            $('#body').width(width);
        }
    });
}

function resizeWindowFromMenu() {
    var height = parseInt($('#heightText').val());
    var width = parseInt($('#widthText').val());
    if (isNaN(width) || isNaN(height)) {
        $('#errorDialog').show();
    }
    else {
        resizeWindow(width, height);
    }
}


// C# interop event handlers:

function onCardPlayed(result) {
    var Card = JSON.parse(result.CardJSON);
    console.log("Player moved " + Card.Name + " from hand to table.");
    if ($('#' + Card.ID).length == 0) {
        //card does not exist insert it to table and then change border
        onCardReceived(result);
    }
    $('#' + Card.ID).css('opacity', '0.5');
};

function onCardReceived(result) {
    var card = JSON.parse(result.CardJSON);
    console.log("Player received " + card.Name + " from deck.");
    var path = "Images_renamed/" + card.ID + ".png";
    addImageToBarWithPopover("playerSide", path, card.Name, card.Text, card.ID);
};

function onOpponentCardPlayed(result) {
    var card = JSON.parse(result.CardJSON);
    console.log("Opponent moved " + card.Name + " from hand to table.");
    var path = "Images_renamed/" + card.ID + ".png";
    addImageToBarWithPopover("opponentSide", path, card.Name, card.Text, card.ID);
    $('#' + card.ID).css('opacity', '0.5');
}

function changeOpponentDeckVisibility() {
    if ($('#opponentDeckBtn').hasClass('active')) {
        $('#opponentSide').show();
        $('#opponentDeckBtn').removeClass('active');
    }
    else {
        $('#opponentSide').hide();
        $('#opponentDeckBtn').addClass('active');
    }
}


// Overwolf API event handlers:

function onGameInfoUpdate(gameInfoChangeDataObject) {
    // TODO: Make sure the game is Hearthstone using the game id:
    console.log("onGameInfoUpdated fired.");
    if (gameInfoChangeDataObject != null && gameInfoChangeDataObject.gameInfo != null &&
        (gameInfoChangeDataObject.gameInfo.id == 98981 || gameInfoChangeDataObject.gameInfo.id == 98982)) {
        var gameInfoOBject = gameInfoChangeDataObject.gameInfo;

        if (gameInfoChangeDataObject.runningChanged || gameInfoChangeDataObject.gameChanged) {
            console.log("onGameInfoUpdate: Game state changed.");
            if (gameInfoOBject.isRunning) {
                // Game turned on:
                console.log('Game turned ON.');
                sampleLibraryObj.StartWorkerThread(genericCallback);
                resizeWindow(gameInfoOBject.width, gameInfoOBject.height);
            }
            else {
                // Game turned off:
                console.log('Game turned OFF.');
                sampleLibraryObj.StopWorkerThread(genericCallback);
                // Remove all cards from trackers:
                $('#playerSide>div').remove('.thumbnail');
                $('#opponentSide>div').remove('.thumbnail');
            }
        }
        if (gameInfoChangeDataObject.resolutionChanged) {
            // Resolution changed:
            console.log("onGameInfoUpdate: Resolution changed.");
            resizeWindow(gameInfoOBject.width, gameInfoOBject.height);
        }
    }
};