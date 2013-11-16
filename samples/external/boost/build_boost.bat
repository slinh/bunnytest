pushd boost_1_52_0\
call bootstrap.bat
b2 link=static threading=multi runtime-link=static address-model=32 --stagedir=./stage/win32 --with-program_options --with-chrono --with-thread --with-date_time
b2 link=static threading=multi runtime-link=static address-model=64 --stagedir=./stage/x64 --with-program_options --with-chrono --with-thread --with-date_time
popd
