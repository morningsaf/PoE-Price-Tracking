dotnet publish -c Release -o publish

xcopy /E /Y scripts publish\scripts\
copy models.py publish\

if exist python-embed\ (
    xcopy /E /Y python-embed\* publish\python\
)
pip install requests sqlalchemy --target publish\python
echo Lib >> publish\python\python311._pth