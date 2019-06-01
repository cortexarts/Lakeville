
Next Gen Recorder Free v0.9.7.8 beta
====================================

Next Gen Recorder is a video recording library that takes the gameplay recording to the next level. Currently it is available for iOS, tvOS and macOS platforms when using Metal or OpenGL renderer.

Documentation
=============

Documentation is provided as a PDF document inside the NextGenRecorder/Documentation folder.

Website
=======

See online documentation and more about the Next Gen Recorder at http://www.pmjo.org/nextgenrecorder

Support
=======

In case you are having problems with Next Gen Recorder, don't hesitate to contact support@pmjo.org

Version history
===============

0.9.7.8:

- Improved audio sync when pause/resume functionality used
- RecordingStopped event is no longer triggered when entering background (internal stop)
- RecordingStarted event is no longer triggered when returning from the background (internal start)
- Fixed audio sync of the exported video when the user has entered background during the recording
- Always round scaled resolution to nearest divisible of 16 to avoid green lines in the exported video
- Updated WebCamRecorder example, will now check recording permission before starting

0.9.7.7:

- Added support for changing Virtual Screen properties in runtime (BlitToScreen and BlitToRecorder)
- Added RecordAudio property to the Recorder to allow disabling audio recording. RecordAudio is automatically set to false if no AudioListeners or custom audio recorders are found.

0.9.7.6:

- Fixed a possible crash when returning from the background and exporting the video, update highly recommended

0.9.7.5:

- SimpleRecorder example scene and prefab was added. Just import the package and drag the SimpleRecorder prefab to your hierarchy root to test the recording!
- WebCamRecorder example scene was added
- Ifdeffed ImageEffectRecorder for iOS, tvOS and macOS only

0.9.7.4:

- Fixed black video issue with OpenGL when UI Mask or stencil buffers are used in the project
- Virtual Screen blit camera is now automatically removed when last Virtual Screen is removed
- Added SavedToPhotos event to Sharing API that gets triggered when saving of a video has completed (or failed)
- Added a possibility to define an album name when saving to the photos (instead of just saving to Camera Roll)

0.9.7.3:

- Fixed a regression with Virtual Screen that caused empty videos when Virtual Screen was added before Next Gen Recorder is initialized. Also fixes similar issue with command buffers.

0.9.7.2:

- First public release
