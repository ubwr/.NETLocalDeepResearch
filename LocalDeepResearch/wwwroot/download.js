/**
 * Downloads a file from a byte stream.
 * @param fileName Name of file being downloaded
 * @param contentStreamReference Byte stream
 */
window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement("a");
    anchorElement.href = url;
    anchorElement.download = fileName;
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
};
