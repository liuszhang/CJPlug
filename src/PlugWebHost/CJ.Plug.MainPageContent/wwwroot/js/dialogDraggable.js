window.makeDialogDraggable = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) return;

    let isDragging = false;
    let offsetX, offsetY;

    const mouseDownHandler = function (e) {
        isDragging = true;
        offsetX = e.clientX - element.offsetLeft;
        offsetY = e.clientY - element.offsetTop;
        element.style.cursor = 'grabbing';
    };

    const mouseMoveHandler = function (e) {
        if (!isDragging) return;
        e.preventDefault();
        const x = e.clientX - offsetX;
        const y = e.clientY - offsetY;
        element.style.position = 'absolute';
        element.style.top = `${y}px`;
        element.style.left = `${x}px`;
    };

    const mouseUpHandler = function () {
        isDragging = false;
        element.style.cursor = 'default';
    };

    element.addEventListener('mousedown', mouseDownHandler);
    document.addEventListener('mousemove', mouseMoveHandler);
    document.addEventListener('mouseup', mouseUpHandler);

    // 清理事件监听器
    element._cleanUp = function () {
        element.removeEventListener('mousedown', mouseDownHandler);
        document.removeEventListener('mousemove', mouseMoveHandler);
        document.removeEventListener('mouseup', mouseUpHandler);
    };
};