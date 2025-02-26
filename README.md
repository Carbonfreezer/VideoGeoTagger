# VideoGeoTagger
Program to geo-tag a video with a Gpx file that has not been shot continuously.

The program reads in an MP4 and a Gpx file. In a second step in the Video timeline, the points where the video is time discontinuous, meaning there are cuts, are marked. This way, the user can split the video into segments. In the third step, the user identifies one correspondent point per video segment on the Gpx track. In the last step, a new GPX file gets sampled and synchronized for the complete video.