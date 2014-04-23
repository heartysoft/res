$framework = '4.0'
properties {
    $config= if($config -eq $null) {'Debug' } else {$config}
    $base_dir = resolve-path .\..
    $source_dir = "$base_dir\src"
    $tools_dir = "$base_dir\tools"

    $res_dir = "$source_dir\res"
    $res_artefacts_dir="$res_dir\Res\bin\$config"
    $resclient_artefacts_dir="$res_dir\Res.Client\bin\$config"
    $res_test_dir = "$res_dir\Res.Core.Tests\bin\$config"
    $test_results_dir="$base_dir\test-results"
    $package_dir = "$base_dir\deploy\$config"
}




task default -depends local

task local -depends package-client, package-server

task clean {
    rd $res_artefacts_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $res_artefacts_dir  -ErrorAction SilentlyContinue  | out-null
}

task compile -depends clean {
    echo $config
    echo $res_artefacts_dir
    exec { msbuild $res_dir\Res.sln /t:Clean /t:Build /p:Configuration=$config /v:q /nologo }
}

task test -depends compile {    
    $testassemblies = get-childitem $res_test_dir -recurse -include *tests*.dll
    mkdir $test_results_dir  -ErrorAction SilentlyContinue  | out-null
    exec { 
        & $tools_dir\NUnit2.6.3\nunit-console-x86.exe $testassemblies /nologo /nodots /xml=$test_results_dir\res.core.tests_results.xml; 
    }
}

task package-server -depends test {
    rd $package_dir\res -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $package_dir\res -ErrorAction SilentlyContinue  | out-null
    
    #$resassemblies = get-childitem $res_artefacts_dir -recurse -include *.dll
    
    exec {
        copy-item $res_artefacts_dir\* $package_dir\res 
    }
}

task package-client -depends test {
    rd $package_dir\res.client -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $package_dir\res.client -ErrorAction SilentlyContinue  | out-null
    
    #$resclientassemblies = get-childitem $resclient_artefacts_dir -recurse -include *.dll
    
    exec {
        copy-item $resclient_artefacts_dir\Res* $package_dir\res.client
    }

}